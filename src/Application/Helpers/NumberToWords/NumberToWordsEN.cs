namespace Application.Helpers.NumberToWords;

public class NumberToWordsEN : INumberToWords
{
    private readonly IOnesToWords _onesToWords;
    private readonly ITensToWords _tensToWords;
    private readonly IHundredsToWords _hundredsToWords;
    private readonly IThousandsToWords _thousandsToWords;
    private readonly IMillionsToWords _millionsToWords;

    public NumberToWordsEN(IOnesToWords onesToWords, ITensToWords tensToWords,
        IHundredsToWords hundredsToWords, IThousandsToWords thousandsToWords, IMillionsToWords millionsToWords)
    {
        _onesToWords = onesToWords;
        _tensToWords = tensToWords;
        _hundredsToWords = hundredsToWords;
        _thousandsToWords = thousandsToWords;
        _millionsToWords = millionsToWords;
    }

    public string OnesSplit(int number, bool hasBefore) => _onesToWords.OnesSplit(number, hasBefore);

    public string TensSplit(int number, bool hasBefore) => _tensToWords.TensSplit(number, hasBefore);

    public string HundredsSplit(int number, bool hasBefore) => _hundredsToWords.HundredsSplit(number, hasBefore);

    public string ThousandsSplit(int number, bool hasBefore) => _thousandsToWords.ThousandsSplit(number, hasBefore);

    public string MillionsSplit(int number) => _millionsToWords.MillionsSplit(number);
}

public class OnesToWordsEN : IOnesToWords
{
    public string OnesSplit(int number, bool hasBefore)
    {
        if (number == 0 && hasBefore)
            return string.Empty;

        string text = hasBefore ? " " : string.Empty;

        text += number switch
        {
            0 => "zero",
            1 => "one",
            2 => "two",
            3 => "three",
            4 => "four",
            5 => "five",
            6 => "six",
            7 => "seven",
            8 => "eight",
            9 => "nine",
            _ => throw new ArgumentException($"OnesSplitEN got {number}"),
        };

        return text;
    }
}

public class TensToWordsEN : ITensToWords
{
    private readonly IOnesToWords _onesToWordsEN;

    public TensToWordsEN(IOnesToWords onesToWords)
    {
        _onesToWordsEN = onesToWords;
    }

    public string TensSplit(int number, bool hasBefore)
    {
        if (number >= 100 || number < 0)
            throw new ArgumentException($"TensSplitEN got {number}");

        bool addWhitespace = hasBefore;
        string text = string.Empty;
        int tens = number / 10;
        int ones = number % 10;

        if (tens > 0)
        {
            text = hasBefore ? " " : string.Empty;
            addWhitespace = true;

            if (tens == 1)
            {
                text += ones switch
                {
                    0 => "ten",
                    1 => "eleven",
                    2 => "twelve",
                    3 => "thirteen",
                    4 => "fourteen",
                    5 => "fifteen",
                    6 => "sixteen",
                    7 => "seventeen",
                    8 => "eighteen",
                    9 => "nineteen",
                    _ => throw new ArgumentException($"TensSplitEN got {number} to parse"),
                };
                return text;
            }

            text += tens switch
            {
                2 => "twenty",
                3 => "thirty",
                4 => "forty",
                5 => "fifty",
                6 => "sixty",
                7 => "seventy",
                8 => "eighty",
                9 => "ninety",
                _ => throw new ArgumentException($"TensSplitEN got {number} to parse"),
            };

            if (ones == 0)
                return text;
        }

        text += _onesToWordsEN.OnesSplit(ones, addWhitespace);

        return text;
    }
}

public class HundredsToWordsEN : IHundredsToWords
{
    private readonly IOnesToWords _onesToWordsEN;
    private readonly ITensToWords _tensToWordsEN;

    public HundredsToWordsEN(IOnesToWords onesToWords, ITensToWords tensToWords)
    {
        _onesToWordsEN = onesToWords;
        _tensToWordsEN = tensToWords;
    }

    public string HundredsSplit(int number, bool hasBefore)
    {
        if (number >= 1000 || number < 0)
            throw new ArgumentException($"HundredsSplitEN got {number}");

        bool addWhitespace = hasBefore;
        string text = string.Empty;
        int hundreds = number / 100;

        if (hundreds > 0)
        {
            text = hasBefore ? " " : string.Empty;
            addWhitespace = true;
            text += _onesToWordsEN.OnesSplit(hundreds, false) + " hundred";
        }

        int tens = number % 100;
        text += _tensToWordsEN.TensSplit(tens, addWhitespace);

        return text;
    }
}

public class ThousandsToWordsEN : IThousandsToWords
{
    private readonly IHundredsToWords _hundredsToWords;

    public ThousandsToWordsEN(IHundredsToWords hundredsToWords)
    {
        _hundredsToWords = hundredsToWords;
    }

    public string ThousandsSplit(int number, bool hasBefore)
    {
        if (number >= 1000000 || number < 0)
            throw new ArgumentException($"ThousandsSplitEN got {number}");

        bool addWhitespace = hasBefore;
        string text = string.Empty;
        int thousands = number / 1000;

        if (thousands > 0)
        {
            text = hasBefore ? " " : string.Empty;
            addWhitespace = true;
            // use hundreds splitter for thousands chunk
            text += _hundredsToWords.HundredsSplit(thousands, false) + " thousand";
        }

        int hundreds = number % 1000;
        text += _hundredsToWords.HundredsSplit(hundreds, addWhitespace);

        return text;
    }
}

public class MillionsToWordsEN : IMillionsToWords
{
    private readonly IHundredsToWords _hundredsToWords;
    private readonly IThousandsToWords _thousandsToWords;

    public MillionsToWordsEN(IHundredsToWords hundredsToWords, IThousandsToWords thousandsToWords)
    {
        _hundredsToWords = hundredsToWords;
        _thousandsToWords = thousandsToWords;
    }

    public string MillionsSplit(int number)
    {
        bool addWhitespace = false;
        string text = string.Empty;
        int millions = number / 1000000;

        if (number >= 1000000000 || number < 0)
            throw new ArgumentException($"MillionsToWordsEN got {number}");

        if (millions > 0)
        {
            addWhitespace = true;
            // use hundreds splitter for millions chunk
            text += _hundredsToWords.HundredsSplit(millions, false) + " million";
        }

        int thousands = number % 1000000;
        text += _thousandsToWords.ThousandsSplit(thousands, addWhitespace);

        return text;
    }
}
