namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal static class GitDeltaResolver
{
    public static byte[] ApplyDelta(byte[] baseData, byte[] delta)
    {
        var index = 0;
        var sourceLength = ReadDeltaVarInt(delta, delta.Length, ref index);
        if (sourceLength != (ulong)baseData.Length)
        {
            throw new InvalidDataException("Delta source length does not match base object length.");
        }

        var resultLength = ReadDeltaVarInt(delta, delta.Length, ref index);
        if (resultLength > int.MaxValue)
        {
            throw new InvalidDataException("Delta result is too large.");
        }

        var result = new byte[(int)resultLength];
        ApplyDeltaCore(baseData, baseData.Length, delta, delta.Length, index, result, result.Length);
        return result;
    }

    private static void ApplyDeltaCore(
        byte[] baseData,
        int baseLength,
        byte[] delta,
        int deltaLength,
        int index,
        byte[] result,
        int resultLength)
    {
        var resultIndex = 0;
        while (index < deltaLength)
        {
            var opcode = delta[index++];
            if ((opcode & 0x80) != 0)
            {
                var copyOffset = 0;
                var copyLength = 0;

                if ((opcode & 0x01) != 0) copyOffset |= ReadDeltaInstructionByte(delta, deltaLength, ref index);
                if ((opcode & 0x02) != 0) copyOffset |= ReadDeltaInstructionByte(delta, deltaLength, ref index) << 8;
                if ((opcode & 0x04) != 0) copyOffset |= ReadDeltaInstructionByte(delta, deltaLength, ref index) << 16;
                if ((opcode & 0x08) != 0) copyOffset |= ReadDeltaInstructionByte(delta, deltaLength, ref index) << 24;
                if ((opcode & 0x10) != 0) copyLength |= ReadDeltaInstructionByte(delta, deltaLength, ref index);
                if ((opcode & 0x20) != 0) copyLength |= ReadDeltaInstructionByte(delta, deltaLength, ref index) << 8;
                if ((opcode & 0x40) != 0) copyLength |= ReadDeltaInstructionByte(delta, deltaLength, ref index) << 16;
                if (copyLength == 0) copyLength = 0x10000;

                if (copyOffset < 0
                    || copyLength < 0
                    || copyOffset > baseLength
                    || copyLength > baseLength - copyOffset)
                {
                    throw new InvalidDataException("Delta copy instruction is outside the base object.");
                }

                if (resultIndex > resultLength || copyLength > resultLength - resultIndex)
                {
                    throw new InvalidDataException("Delta copy instruction is outside the result object.");
                }

                baseData.AsSpan(copyOffset, copyLength)
                    .CopyTo(result.AsSpan(resultIndex));
                resultIndex += copyLength;
            }
            else if (opcode != 0)
            {
                if (opcode > deltaLength - index)
                {
                    throw new InvalidDataException("Delta insert instruction overruns the delta buffer.");
                }

                if (resultIndex > resultLength || opcode > resultLength - resultIndex)
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

        if (resultIndex != resultLength)
        {
            throw new InvalidDataException("Delta result length does not match expected length.");
        }
    }

    private static int ReadDeltaInstructionByte(byte[] data, int length, ref int index)
    {
        if (index >= length)
        {
            throw new InvalidDataException("Delta instruction overran buffer.");
        }

        return data[index++];
    }

    private static ulong ReadDeltaVarInt(byte[] data, int length, ref int index)
    {
        ulong value = 0;
        var shift = 0;
        while (true)
        {
            if (index >= length)
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
