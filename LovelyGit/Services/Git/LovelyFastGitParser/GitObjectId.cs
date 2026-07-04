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

    public static GitObjectId Parse(ReadOnlySpan<char> value, GitObjectFormat objectFormat)
    {
        if (!TryParse(value, objectFormat, out var id))
        {
            throw new FormatException($"Invalid {objectFormat} Git object id.");
        }

        return id;
    }

    public static GitObjectId ParseAscii(ReadOnlySpan<byte> value, GitObjectFormat objectFormat)
    {
        if (!TryParseAscii(value, objectFormat, out var id))
        {
            throw new FormatException($"Invalid {objectFormat} Git object id.");
        }

        return id;
    }

    public static GitObjectId FromBytes(ReadOnlySpan<byte> value, GitObjectFormat objectFormat)
    {
        if (value.Length != GetByteLength(objectFormat))
        {
            throw new FormatException($"Invalid {objectFormat} Git object id byte length.");
        }

        var chars = new char[value.Length * 2];
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            chars[i * 2] = ToHexChar(current >> 4);
            chars[(i * 2) + 1] = ToHexChar(current & 0x0f);
        }

        return new GitObjectId(new string(chars), objectFormat);
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
            if (!IsHex(value[i]))
            {
                return false;
            }
        }

        id = new GitObjectId(value.ToLowerInvariant(), objectFormat);
        return true;
    }

    public static bool TryParse(ReadOnlySpan<char> value, GitObjectFormat objectFormat, out GitObjectId id)
    {
        id = default;
        if (value.Length != GetTextLength(objectFormat))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (!IsHex(value[i]))
            {
                return false;
            }
        }

        id = new GitObjectId(value.ToString().ToLowerInvariant(), objectFormat);
        return true;
    }

    public static bool TryParseAscii(ReadOnlySpan<byte> value, GitObjectFormat objectFormat, out GitObjectId id)
    {
        id = default;
        if (value.Length != GetTextLength(objectFormat))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (!IsHex(value[i]))
            {
                return false;
            }
        }

        var text = string.Create(value.Length, value, static (destination, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var current = source[i];
                destination[i] = (char)(current >= (byte)'A' && current <= (byte)'F'
                    ? current + 32
                    : current);
            }
        });
        id = new GitObjectId(text, objectFormat);
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
            destination[i] = (byte)((FromHex(Value[i * 2]) << 4) | FromHex(Value[(i * 2) + 1]));
        }
    }

    private static bool IsHex(char value)
    {
        return value is >= '0' and <= '9'
            or >= 'a' and <= 'f'
            or >= 'A' and <= 'F';
    }

    public static bool IsHexPrefix(string value)
    {
        return value.Length == 2 && IsHex(value[0]) && IsHex(value[1]);
    }

    private static bool IsHex(byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9'
            or >= (byte)'a' and <= (byte)'f'
            or >= (byte)'A' and <= (byte)'F';
    }

    private static int FromHex(char value)
    {
        if (value <= '9')
        {
            return value - '0';
        }

        return (value | 0x20) - 'a' + 10;
    }

    private static char ToHexChar(int value)
    {
        return (char)(value < 10 ? '0' + value : 'a' + value - 10);
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
