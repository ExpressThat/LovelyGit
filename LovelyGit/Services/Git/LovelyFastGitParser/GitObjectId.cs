using System.Globalization;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal readonly record struct GitObjectId(string Value, GitObjectFormat ObjectFormat)
{
    public int ByteLength => GetByteLength(ObjectFormat);

    public static GitObjectId Parse(string value)
    {
        if (!TryParse(value, out var id))
        {
            throw new FormatException("Invalid Git object id.");
        }

        return id;
    }

    public static GitObjectId Parse(string value, GitObjectFormat objectFormat)
    {
        if (!TryParse(value, objectFormat, out var id))
        {
            throw new FormatException($"Invalid {objectFormat} Git object id.");
        }

        return id;
    }

    public static bool TryParse(string? value, out GitObjectId id)
    {
        id = default;
        if (value == null)
        {
            return false;
        }

        return value.Length switch
        {
            40 => TryParse(value, GitObjectFormat.Sha1, out id),
            64 => TryParse(value, GitObjectFormat.Sha256, out id),
            _ => false,
        };
    }

    public static bool TryParse(string? value, GitObjectFormat objectFormat, out GitObjectId id)
    {
        id = default;
        if (value == null || value.Length != GetTextLength(objectFormat))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
            {
                return false;
            }
        }

        id = new GitObjectId(value.ToLowerInvariant(), objectFormat);
        return true;
    }

    public byte[] ToByteArray()
    {
        var bytes = new byte[ByteLength];
        WriteTo(bytes);
        return bytes;
    }

    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < ByteLength)
        {
            throw new ArgumentException("Destination is too small.", nameof(destination));
        }

        for (var i = 0; i < ByteLength; i++)
        {
            destination[i] = byte.Parse(Value.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }

    public static int GetByteLength(GitObjectFormat objectFormat) => objectFormat switch
    {
        GitObjectFormat.Sha1 => 20,
        GitObjectFormat.Sha256 => 32,
        _ => throw new ArgumentOutOfRangeException(nameof(objectFormat), objectFormat, null),
    };

    public static int GetTextLength(GitObjectFormat objectFormat)
    {
        return GetByteLength(objectFormat) * 2;
    }

    public override string ToString() => Value;
}

internal enum GitObjectFormat
{
    Sha1,
    Sha256,
}
