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

                if ((opcode & 0x01) != 0) copyOffset |= ReadDeltaInstructionByte(delta, ref index);
                if ((opcode & 0x02) != 0) copyOffset |= ReadDeltaInstructionByte(delta, ref index) << 8;
                if ((opcode & 0x04) != 0) copyOffset |= ReadDeltaInstructionByte(delta, ref index) << 16;
                if ((opcode & 0x08) != 0) copyOffset |= ReadDeltaInstructionByte(delta, ref index) << 24;
                if ((opcode & 0x10) != 0) copyLength |= ReadDeltaInstructionByte(delta, ref index);
                if ((opcode & 0x20) != 0) copyLength |= ReadDeltaInstructionByte(delta, ref index) << 8;
                if ((opcode & 0x40) != 0) copyLength |= ReadDeltaInstructionByte(delta, ref index) << 16;
                if (copyLength == 0) copyLength = 0x10000;

                if (copyOffset < 0
                    || copyLength < 0
                    || copyOffset > baseData.Length
                    || copyLength > baseData.Length - copyOffset)
                {
                    throw new InvalidDataException("Delta copy instruction is outside the base object.");
                }

                if (resultIndex > result.Length || copyLength > result.Length - resultIndex)
                {
                    throw new InvalidDataException("Delta copy instruction is outside the result object.");
                }

                baseData.AsSpan(copyOffset, copyLength)
                    .CopyTo(result.AsSpan(resultIndex));
                resultIndex += copyLength;
            }
            else if (opcode != 0)
            {
                if (opcode > delta.Length - index)
                {
                    throw new InvalidDataException("Delta insert instruction overruns the delta buffer.");
                }

                if (resultIndex > result.Length || opcode > result.Length - resultIndex)
                {
                    throw new InvalidDataException("Delta insert instruction is outside the result object.");
                }

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

    private static int ReadDeltaInstructionByte(byte[] data, ref int index)
    {
        if (index >= data.Length)
        {
            throw new InvalidDataException("Delta instruction overran buffer.");
        }

        return data[index++];
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
