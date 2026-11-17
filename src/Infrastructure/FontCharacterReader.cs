using System.Buffers.Binary;

namespace Infrastructure;

/// <summary>
/// Reads which characters a TrueType/OpenType font file can draw, from its "cmap" table: the
/// Unicode subtables of format 12 (every plane) when it has one, else those of format 4 (the basic
/// plane), as a text shaper picks them. QuestPDF has no such lookup.
/// </summary>
public static class FontCharacterReader
{
    private const int MaxCodePoint = 0x10FFFF;

    /// <summary>
    /// The Unicode code points the font has a glyph for
    /// </summary>
    /// <param name="font">the font file's bytes</param>
    /// <param name="path">the file's path, for the error message</param>
    /// <exception cref="ArgumentException">the bytes aren't a font with a Unicode character table</exception>
    public static HashSet<int> Read(ReadOnlySpan<byte> font, string path)
    {
        HashSet<int> fullCodePoints = [];
        HashSet<int> basicCodePoints = [];
        try
        {
            ReadOnlySpan<byte> cmap = FindTable(font, "cmap"u8);

            int subtableCount = U16(cmap, 2);
            for (int i = 0; i < subtableCount; i++)
            {
                int record = 4 + 8 * i;
                int platform = U16(cmap, record);
                int encoding = U16(cmap, record + 2);
                // Unicode subtables: platform 0, or Windows (3) with encoding 1 (BMP) or 10 (full)
                if (platform != 0 && !(platform == 3 && (encoding == 1 || encoding == 10)))
                    continue;

                ReadOnlySpan<byte> subtable = cmap[checked((int)U32(cmap, record + 4))..];
                switch (U16(subtable, 0))
                {
                    case 4: ReadFormat4(subtable, basicCodePoints); break;
                    case 12: ReadFormat12(subtable, fullCodePoints); break;
                }
            }
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException or OverflowException)
        {
            throw new ArgumentException($"Font file is damaged or not a font: {path}", e);
        }

        HashSet<int> codePoints = fullCodePoints.Count != 0 ? fullCodePoints : basicCodePoints;
        if (codePoints.Count == 0)
            throw new ArgumentException($"Font file has no Unicode character table: {path}");

        return codePoints;
    }

    private static ReadOnlySpan<byte> FindTable(ReadOnlySpan<byte> font, ReadOnlySpan<byte> tag)
    {
        int tableCount = U16(font, 4);
        for (int i = 0; i < tableCount; i++)
        {
            int record = 12 + 16 * i;
            if (font.Slice(record, 4).SequenceEqual(tag))
                return font.Slice(checked((int)U32(font, record + 8)), checked((int)U32(font, record + 12)));
        }

        throw new ArgumentOutOfRangeException(nameof(font), "no cmap table");
    }

    // Segments of [start, end]. A glyph id comes from the delta, or through the glyph id array.
    private static void ReadFormat4(ReadOnlySpan<byte> table, HashSet<int> codePoints)
    {
        int segmentCount = U16(table, 6) / 2;
        int endCodes = 14;
        int startCodes = endCodes + 2 * segmentCount + 2;
        int deltas = startCodes + 2 * segmentCount;
        int rangeOffsets = deltas + 2 * segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            int start = U16(table, startCodes + 2 * i);
            int end = U16(table, endCodes + 2 * i);
            int delta = U16(table, deltas + 2 * i);
            int rangeOffsetAt = rangeOffsets + 2 * i;
            int rangeOffset = U16(table, rangeOffsetAt);

            for (int codePoint = start; codePoint <= end && codePoint < 0xFFFF; codePoint++)
            {
                int glyph;
                if (rangeOffset == 0)
                {
                    glyph = (codePoint + delta) & 0xFFFF;
                }
                else
                {
                    glyph = U16(table, rangeOffsetAt + rangeOffset + 2 * (codePoint - start));
                    if (glyph != 0)
                        glyph = (glyph + delta) & 0xFFFF;
                }

                if (glyph != 0)
                    codePoints.Add(codePoint);
            }
        }
    }

    // Groups of [start, end] whose glyph ids count up from the group's first glyph id
    private static void ReadFormat12(ReadOnlySpan<byte> table, HashSet<int> codePoints)
    {
        uint groupCount = U32(table, 12);
        for (uint i = 0; i < groupCount; i++)
        {
            int group = checked((int)(16 + 12 * i));
            uint start = U32(table, group);
            uint end = Math.Min(U32(table, group + 4), MaxCodePoint);
            uint firstGlyph = U32(table, group + 8);

            for (uint codePoint = start; codePoint <= end; codePoint++)
            {
                if (firstGlyph + (codePoint - start) != 0)
                    codePoints.Add((int)codePoint);
            }
        }
    }

    private static int U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));

    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
}
