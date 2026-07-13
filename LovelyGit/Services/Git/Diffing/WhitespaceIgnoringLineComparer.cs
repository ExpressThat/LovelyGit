namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal sealed class WhitespaceIgnoringLineComparer : IEqualityComparer<string>
{
    public static readonly WhitespaceIgnoringLineComparer Instance = new();

    public bool Equals(string? left, string? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (string.Equals(left, right, StringComparison.Ordinal)) return true;
        var leftIndex = 0;
        var rightIndex = 0;
        while (true)
        {
            while (leftIndex < left.Length && char.IsWhiteSpace(left[leftIndex])) leftIndex++;
            while (rightIndex < right.Length && char.IsWhiteSpace(right[rightIndex])) rightIndex++;
            if (leftIndex == left.Length || rightIndex == right.Length)
                return leftIndex == left.Length && rightIndex == right.Length;
            if (left[leftIndex++] != right[rightIndex++]) return false;
        }
    }

    public int GetHashCode(string value)
    {
        var hash = new HashCode();
        foreach (var character in value)
        {
            if (!char.IsWhiteSpace(character)) hash.Add(character);
        }
        return hash.ToHashCode();
    }
}
