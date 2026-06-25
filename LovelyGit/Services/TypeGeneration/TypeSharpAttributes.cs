namespace ExpressThat.LovelyGit.Services.TypeGeneration;

[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Enum)]
public sealed class TypeSharpAttribute : Attribute
{
    public TypeSharpAttribute()
    {
    }

    public TypeSharpAttribute(string name)
    {
        Name = name;
    }

    public string? Name { get; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TypeAsAttribute(string typeName) : Attribute
{
    public string TypeName { get; } = typeName;
}

[AttributeUsage(AttributeTargets.Enum)]
public sealed class UnionAttribute : Attribute;
