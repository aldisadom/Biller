using AwesomeAssertions;
using Infrastructure;
using System.Buffers.Binary;

namespace xUnitTests.Infrastructure;

public class FontCharacterReaderTest
{
    // A font file with only a cmap table that has the given subtables
    internal static byte[] FontWithSubtables(params (ushort Platform, ushort Encoding, byte[] Subtable)[] subtables)
    {
        const int cmapOffset = 12 + 16;
        int subtableOffset = 4 + 8 * subtables.Length;

        byte[] font = new byte[cmapOffset + subtableOffset + subtables.Sum(s => s.Subtable.Length)];
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(0), 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(4), 1);
        "cmap"u8.CopyTo(font.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(20), cmapOffset);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(24), (uint)(font.Length - cmapOffset));

        Span<byte> cmap = font.AsSpan(cmapOffset);
        BinaryPrimitives.WriteUInt16BigEndian(cmap[2..], (ushort)subtables.Length);
        for (int i = 0; i < subtables.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(cmap[(4 + 8 * i)..], subtables[i].Platform);
            BinaryPrimitives.WriteUInt16BigEndian(cmap[(6 + 8 * i)..], subtables[i].Encoding);
            BinaryPrimitives.WriteUInt32BigEndian(cmap[(8 + 8 * i)..], (uint)subtableOffset);
            subtables[i].Subtable.CopyTo(cmap[subtableOffset..]);
            subtableOffset += subtables[i].Subtable.Length;
        }

        return font;
    }

    private static byte[] FontWithSubtable(ushort platform, ushort encoding, byte[] subtable) => FontWithSubtables((platform, encoding, subtable));

    // Format 4 with one segment, start..end, whose glyph ids are the code points plus 1, and the
    // segment for 0xFFFF that ends every format 4 table
    private static byte[] Format4(ushort start, ushort end)
    {
        ushort[] values = [4, 32, 0, 4, 0, 0, 0, end, 0xFFFF, 0, start, 0xFFFF, 1, 1, 0, 0];
        byte[] subtable = new byte[values.Length * 2];
        for (int i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteUInt16BigEndian(subtable.AsSpan(2 * i), values[i]);

        return subtable;
    }

    // Format 12 with one group: code points start..end get glyph ids counting up from firstGlyph
    internal static byte[] Format12(uint start, uint end, uint firstGlyph)
    {
        byte[] subtable = new byte[16 + 12];
        BinaryPrimitives.WriteUInt16BigEndian(subtable.AsSpan(0), 12);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(4), (uint)subtable.Length);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(16), start);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(20), end);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(24), firstGlyph);

        return subtable;
    }

    [Theory]
    [InlineData("AaZz09 .,-/()€", Label = "calibri has latin letters, digits, punctuation and the euro sign")]
    [InlineData("ąčęėįšųūžĄČĘĖĮŠŲŪŽ", Label = "calibri has every lithuanian letter")]
    public void Read_GivenCalibri_HasCharacters(string characters)
    {
        //Arrange
        byte[] font = File.ReadAllBytes(RepoFonts.CalibriPath);

        //Act
        HashSet<int> result = FontCharacterReader.Read(font, RepoFonts.CalibriPath);

        //Assert
        result.Should().Contain(characters.Select(c => (int)c));
    }

    [Theory]
    [InlineData(0x1F409, Label = "calibri has no dragon emoji")]
    [InlineData(0x9435, Label = "calibri has no chinese character")]
    [InlineData(0xFFFF, Label = "the code point that ends the table is not a character")]
    public void Read_GivenCalibri_DoesNotHaveCharacter(int codePoint)
    {
        //Arrange
        byte[] font = File.ReadAllBytes(RepoFonts.CalibriPath);

        //Act
        HashSet<int> result = FontCharacterReader.Read(font, RepoFonts.CalibriPath);

        //Assert
        result.Should().NotContain(codePoint);
    }

    [Fact]
    public void Read_GivenFormat12Subtable_ReturnsItsGroupWithoutTheMissingGlyph()
    {
        //Arrange
        // glyph ids 0, 1, 2 for the three code points: glyph 0 is the "missing glyph"
        byte[] font = FontWithSubtable(3, 10, Format12(0x1F408, 0x1F40A, 0));

        //Act
        HashSet<int> result = FontCharacterReader.Read(font, "dragons.ttf");

        //Assert
        result.Should().BeEquivalentTo([0x1F409, 0x1F40A]);
    }

    [Fact]
    public void Read_GivenFormat4Subtable_ReturnsItsSegment()
    {
        //Arrange
        byte[] font = FontWithSubtable(3, 1, Format4(0x41, 0x43));

        //Act
        HashSet<int> result = FontCharacterReader.Read(font, "abc.ttf");

        //Assert
        result.Should().BeEquivalentTo([0x41, 0x42, 0x43]);
    }

    // A text shaper uses the full table when a font has both, so a character only the basic table
    // has would be allowed here and still fail the PDF
    [Fact]
    public void Read_GivenFormat4AndFormat12Subtables_ReturnsOnlyTheFormat12Characters()
    {
        //Arrange
        byte[] font = FontWithSubtables((3, 1, Format4(0x41, 0x43)), (3, 10, Format12(0x41, 0x42, 1)));

        //Act
        HashSet<int> result = FontCharacterReader.Read(font, "both.ttf");

        //Assert
        result.Should().BeEquivalentTo([0x41, 0x42]);
    }

    [Fact]
    public void Read_GivenOnlyANonUnicodeSubtable_ThrowsArgumentExceptionWithPath()
    {
        //Arrange
        // platform 1 (Macintosh) is not a Unicode table
        byte[] font = FontWithSubtable(1, 0, Format12(0x41, 0x5A, 1));

        //Act
        Action act = () => FontCharacterReader.Read(font, "old-mac.ttf");

        //Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Font file has no Unicode character table: old-mac.ttf");
    }

    public static IEnumerable<TheoryDataRow<byte[]>> GetDamagedFontTestData()
    {
        byte[] calibri = File.ReadAllBytes(RepoFonts.CalibriPath);

        yield return new(Array.Empty<byte>()) { Label = "empty file throws" };
        yield return new("Winter is coming"u8.ToArray()) { Label = "text file throws" };
        yield return new(calibri[..1000]) { Label = "font file cut off before its character table throws" };
    }

    [Theory]
    [MemberData(nameof(GetDamagedFontTestData))]
    public void Read_GivenDamagedFile_ThrowsArgumentExceptionWithPath(byte[] font)
    {
        //Act
        Action act = () => FontCharacterReader.Read(font, "broken.ttf");

        //Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Font file is damaged or not a font: broken.ttf");
    }
}
