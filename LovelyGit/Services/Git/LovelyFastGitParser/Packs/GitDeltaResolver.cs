namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal static class GitDeltaResolver
{
    public static byte[] ApplyDelta(byte[] baseData, byte[] delta)
    {
        var index = 0;
        var sourceLength = ReadDeltaVarInt(delta, ref index);
        if (sourceLength != (ulong)baseData.Length)
        {
            throw new InvalidDataException("Delta source length does not match base object length.");
        }

        var resultLength = ReadDeltaVarInt(delta, ref index);
        if (resultLength > int.MaxValue)
        {
            throw new InvalidDataException("Delta result is too large.");
        }

        var result = new byte[(int)resultLength];
        var resultIndex = 0;

        while (index < delta.Length)
        {
            var opcode = delta[index++];
            if ((opcode & 0x80) != 0)
            {
                var copyOffset = 0;
                var copyLength = 0;

                if ((opcode & 0x01) != 0) copyOffset |= delta[index++];
                if ((opcode & 0x02) != 0) copyOffset |= delta[index++] << 8;
                if ((opcode & 0x04) != 0) copyOffset |= delta[index++] << 16;
                if ((opcode & 0x08) != 0) copyOffset |= delta[index++] << 24;
                if ((opcode & 0x10) != 0) copyLength |= delta[index++];
                if ((opcode & 0x20) != 0) copyLength |= delta[index++] << 8;
                if ((opcode & 0x40) != 0) copyLength |= delta[index++] << 16;
                if (copyLength == 0) copyLength = 0x10000;

                baseData.AsSpan(copyOffset, copyLength)
                    .CopyTo(result.AsSpan(resultIndex));
                resultIndex += copyLength;
            }
            else if (opcode != 0)
            {
                delta.AsSpan(index, opcode)
                    .CopyTo(result.AsSpan(resultIndex));
                index += opcode;
                resultIndex += opcode;
            }
            else
            {
                throw new InvalidDataException("Invalid zero delta opcode.");
            }
        }

        if ((ulong)resultIndex != resultLength)
        {
            throw new InvalidDataException("Delta result length does not match expected length.");
        }

        return result;
    }

    private static ulong ReadDeltaVarInt(byte[] data, ref int index)
    {
        ulong value = 0;
        var shift = 0;
        while (true)
        {
            if (index >= data.Length)
            {
                throw new InvalidDataException("Delta varint overran buffer.");
            }

            var current = data[index++];
            value |= ((ulong)(current & 0x7f)) << shift;
            if ((current & 0x80) == 0)
            {
                return value;
            }

            shift += 7;
        }
    }
}
