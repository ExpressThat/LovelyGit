using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LovelyGit.BLiteGenerators;

[Generator]
public sealed class BsonKeysGenerator : IIncrementalGenerator
{
    private const string DocumentDbContextMetadataName = "BLite.Core.DocumentDbContext";
    private const string DocumentCollectionMetadataName = "BLite.Core.Collections.DocumentCollection";
    private const string ColumnAttributeMetadataName = "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute";
    private const string KeyAttributeMetadataName = "System.ComponentModel.DataAnnotations.KeyAttribute";

    private static readonly DiagnosticDescriptor UnsupportedColumnAttribute = new(
        "LGBSON001",
        "Column name must be a string literal",
        "Property '{0}' has a ColumnAttribute without a constant string name and cannot be included in generated BSON keys",
        "LovelyGit.BLiteGenerators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var dbContexts = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, _) => GetDocumentDbContext(syntaxContext))
            .Where(static symbol => symbol != null)
            .Collect();

        var compilationAndContexts = context.CompilationProvider.Combine(dbContexts);
        context.RegisterSourceOutput(compilationAndContexts, static (sourceContext, source) =>
        {
            var (compilation, contexts) = source;
            Execute(sourceContext, compilation, contexts);
        });
    }

    private static INamedTypeSymbol? GetDocumentDbContext(GeneratorSyntaxContext context)
    {
        var type = (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(context.Node);
        if (type == null || !IsDocumentDbContext(type))
        {
            return null;
        }

        return type;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol?> dbContexts)
    {
        var documentCollectionType = compilation.GetTypeByMetadataName(DocumentCollectionMetadataName + "`2");
        var columnAttributeType = compilation.GetTypeByMetadataName(ColumnAttributeMetadataName);
        var keyAttributeType = compilation.GetTypeByMetadataName(KeyAttributeMetadataName);
        if (documentCollectionType == null || columnAttributeType == null || keyAttributeType == null)
        {
            return;
        }

        var seenDbContexts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dbContext in dbContexts)
        {
            if (dbContext == null || !seenDbContexts.Add(GetSymbolKey(dbContext)))
            {
                continue;
            }

            var keys = new OrderedKeySet();
            var diagnostics = new List<Diagnostic>();

            foreach (var documentType in GetDocumentTypes(dbContext, documentCollectionType))
            {
                CollectKeys(documentType, columnAttributeType, keyAttributeType, keys, diagnostics, new HashSet<string>(StringComparer.Ordinal));
            }

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            context.AddSource(
                GetHintName(dbContext),
                SourceText.From(RenderDbContextPartial(dbContext, keys), Encoding.UTF8));
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetDocumentTypes(
        INamedTypeSymbol dbContext,
        INamedTypeSymbol documentCollectionType)
    {
        foreach (var property in GetPublicInstanceProperties(dbContext))
        {
            if (property.Type is not INamedTypeSymbol namedType ||
                namedType.ConstructedFrom == null ||
                !SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, documentCollectionType) ||
                namedType.TypeArguments.Length != 2 ||
                namedType.TypeArguments[1] is not INamedTypeSymbol documentType)
            {
                continue;
            }

            yield return documentType;
        }
    }

    private static void CollectKeys(
        INamedTypeSymbol type,
        INamedTypeSymbol columnAttributeType,
        INamedTypeSymbol keyAttributeType,
        OrderedKeySet keys,
        List<Diagnostic> diagnostics,
        HashSet<string> visited)
    {
        if (!visited.Add(GetSymbolKey(type)))
        {
            return;
        }

        foreach (var property in GetPublicInstanceProperties(type))
        {
            if (HasAttribute(property, keyAttributeType))
            {
                keys.Add("id");
                keys.Add("_id");
            }

            var columnAttribute = property.GetAttributes()
                .FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, columnAttributeType));
            if (columnAttribute != null)
            {
                var columnName = GetColumnName(columnAttribute);
                if (columnName == null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        UnsupportedColumnAttribute,
                        property.Locations.FirstOrDefault(),
                        property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
                else
                {
                    keys.Add(columnName);
                }
            }
            else if (!HasAttribute(property, keyAttributeType))
            {
                keys.Add(GetDefaultBsonPropertyName(property));
            }

            foreach (var nestedType in GetNestedModelTypes(property.Type))
            {
                CollectKeys(nestedType, columnAttributeType, keyAttributeType, keys, diagnostics, visited);
            }
        }
    }

    private static IEnumerable<IPropertySymbol> GetPublicInstanceProperties(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!member.IsStatic && member.DeclaredAccessibility == Accessibility.Public)
                {
                    yield return member;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedModelTypes(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            yield break;
        }

        if (IsPrimitiveLike(namedType))
        {
            yield break;
        }

        if (namedType.TypeArguments.Length == 1 && IsCollectionType(namedType))
        {
            if (namedType.TypeArguments[0] is INamedTypeSymbol elementType && !IsPrimitiveLike(elementType))
            {
                yield return elementType;
            }

            yield break;
        }

        yield return namedType;
    }

    private static string? GetColumnName(AttributeData columnAttribute)
    {
        if (columnAttribute.ConstructorArguments.Length == 1 &&
            columnAttribute.ConstructorArguments[0].Value is string constructorName)
        {
            return constructorName;
        }

        return null;
    }

    private static string GetDefaultBsonPropertyName(IPropertySymbol property)
    {
        return property.Name.ToLowerInvariant();
    }

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
    {
        return symbol.GetAttributes()
            .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
    }

    private static bool IsDocumentDbContext(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + DocumentDbContextMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPrimitiveLike(INamedTypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None)
        {
            return true;
        }

        var metadataName = type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return metadataName is
            "global::System.Guid" or
            "global::System.DateTime" or
            "global::System.DateTimeOffset" or
            "global::System.TimeSpan" or
            "global::System.Decimal";
    }

    private static bool IsCollectionType(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Array)
        {
            return true;
        }

        var metadataName = type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (metadataName is
            "global::System.Collections.Generic.List<T>" or
            "global::System.Collections.Generic.IList<T>" or
            "global::System.Collections.Generic.IReadOnlyList<T>" or
            "global::System.Collections.Generic.ICollection<T>" or
            "global::System.Collections.Generic.IReadOnlyCollection<T>" or
            "global::System.Collections.Generic.IEnumerable<T>")
        {
            return true;
        }

        return type.AllInterfaces.Any(static item =>
            item.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.IEnumerable<T>");
    }

    private static string RenderDbContextPartial(INamedTypeSymbol dbContext, OrderedKeySet keys)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        var containingTypes = GetContainingTypes(dbContext);
        var namespaceName = dbContext.ContainingNamespace.IsGlobalNamespace
            ? null
            : dbContext.ContainingNamespace.ToDisplayString();

        if (namespaceName != null)
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine(";");
            builder.AppendLine();
        }

        foreach (var containingType in containingTypes)
        {
            builder.Append("partial ").Append(GetTypeKind(containingType)).Append(' ').Append(containingType.Name).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial ").Append(GetTypeKind(dbContext)).Append(' ').Append(dbContext.Name).AppendLine();
        builder.AppendLine("{");
        builder.Append("    private static readonly string[] ").Append(GetBsonKeysFieldName(dbContext)).AppendLine(" =");
        builder.AppendLine("    [");

        foreach (var key in keys.Keys)
        {
            builder.Append("        \"").Append(EscapeString(key)).AppendLine("\",");
        }

        builder.AppendLine("    ];");
        builder.AppendLine("}");

        foreach (var _ in containingTypes)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static ImmutableArray<INamedTypeSymbol> GetContainingTypes(INamedTypeSymbol type)
    {
        var containingTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        for (var containingType = type.ContainingType; containingType != null; containingType = containingType.ContainingType)
        {
            containingTypes.Insert(0, containingType);
        }

        return containingTypes.ToImmutable();
    }

    private static string GetTypeKind(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Struct ? "struct" : "class";
    }

    private static string GetSymbolKey(INamedTypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string GetBsonKeysFieldName(INamedTypeSymbol dbContext)
    {
        return dbContext.Name == "GitRepoCacheDbContext"
            ? "CacheBsonKeys"
            : "AppBsonKeys";
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('.', '_') + ".BsonKeys.g.cs";
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private sealed class OrderedKeySet
    {
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        private readonly List<string> _keys = new();

        public IReadOnlyList<string> Keys => _keys;

        public void Add(string key)
        {
            if (_seen.Add(key))
            {
                _keys.Add(key);
            }
        }
    }
}
