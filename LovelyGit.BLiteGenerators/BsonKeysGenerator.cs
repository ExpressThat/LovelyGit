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
public sealed partial class BsonKeysGenerator : IIncrementalGenerator
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

}
