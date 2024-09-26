namespace Application.Helpers.NumberToWords;

public class NumberToWordsLT : INumberToWords
{
    private readonly IOnesToWords _onesToWords;
    private readonly ITensToWords _tensToWords;
    private readonly IHundredsToWords _hundredsToWords;
    private readonly IThousandsToWords _thousandsToWords;
    private readonly IMillionsToWords _millionsToWords;

    public NumberToWordsLT(IOnesToWords onesToWords, ITensToWords tensToWords,
        IHundredsToWords hundredsToWords, IThousandsToWords thousandsToWords, IMillionsToWords millionsToWords)
    {
        _onesToWords = onesToWords;
        _tensToWords = tensToWords;
        _hundredsToWords = hundredsToWords;
        _thousandsToWords = thousandsToWords;
        _millionsToWords = millionsToWords;
    }

    public string OnesSplit(int number, bool hasBefore)
    {
        return _onesToWords.OnesSplit(number, hasBefore);
    }

    public string TensSplit(int number, bool hasBefore)
    {
        return _tensToWords.TensSplit(number, hasBefore);
    }

    public string HundredsSplit(int number, bool hasBefore)
    {
        return _hundredsToWords.HundredsSplit(number, hasBefore);
    }
    public string ThousandsSplit(int number, bool hasBefore)
    {
        return _thousandsToWords.ThousandsSplit(number, hasBefore);
    }
    public string MillionsSplit(int number)
    {
        return _millionsToWords.MillionsSplit(number);
    }
}

public class OnesToWordsLT : IOnesToWords
{
    public string OnesSplit(int number, bool hasBefore)
    {
        if (number >= 10 || number < 0)
            throw new ArgumentException($"OnesSplitLT got {number}");

        if (number == 0 && hasBefore)
            return string.Empty;

        string text = hasBefore ? " " : string.Empty;

        text += number switch
        {
            0 => "nulis",
            1 => "vienas",
            2 => "du",
            3 => "trys",
            4 => "keturi",
            5 => "penki",
            6 => "šeši",
            7 => "septyni",
            8 => "aštuoni",
            9 => "devyni",
            _ => throw new ArgumentException($"OnesSplitLT got {number}"),
        };

        return text;
    }
}

public class TensToWordsLT : ITensToWords
{
    private readonly IOnesToWords _onesToWordsLT;

    public TensToWordsLT(IOnesToWords onesToWords)
    {
        _onesToWordsLT = onesToWords;
    }

    public string TensSplit(int number, bool hasBefore)
    {
        if (number >= 100 || number < 0)
            throw new ArgumentException($"TensSplitLT got {number}");

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
                text += (number % 10) switch
                {
                    0 => "dešimt",
                    1 => "vienuolika",
                    2 => "dvylika",
                    3 => "trylika",
                    4 => "keturiolika",
                    5 => "penkiolika",
                    6 => "šešiolika",
                    7 => "septyniolika",
                    8 => "aštuoniolika",
                    9 => "devyniolika",
                    _ => throw new NotImplementedException()
                };
                return text;
            }
            else if (tens == 2)
                text += "dvidešimt";
            else if (tens == 3)
                text += "trisdešimt";
            else
                text += _onesToWordsLT.OnesSplit(number / 10, false) + "asdešimt";
        }

        text += _onesToWordsLT.OnesSplit(ones, addWhitespace);

        return text;
    }
}

public class HundredsToWordsLT : IHundredsToWords
{
    private readonly IOnesToWords _onesToWordsLT;
    private readonly ITensToWords _tensToWordsLT;

    public HundredsToWordsLT(IOnesToWords onesToWords, ITensToWords tensToWords)
    {
        _onesToWordsLT = onesToWords;
        _tensToWordsLT = tensToWords;
    }

    public string HundredsSplit(int number, bool hasBefore)
    {
        if (number >= 1000 || number < 0)
            throw new ArgumentException($"HundredsSplitLT got {number}");

        bool addWhitespace = hasBefore;
        string text = string.Empty;
        int hundreds = number / 100;

        if (hundreds > 0)
        {
            text = hasBefore ? " " : string.Empty;
            addWhitespace = true;
            text += _onesToWordsLT.OnesSplit(number / 100, false);
            if (number / 100 == 1)
                text += " šimtas";
            else
                text += " šimtai";
        }

        int tens = number % 100;
        text += _tensToWordsLT.TensSplit(tens, addWhitespace);

        return text;
    }
}

public class ThousandsToWordsLT : IThousandsToWords
{
    private readonly IHundredsToWords _hundredsToWords;

    public ThousandsToWordsLT(IHundredsToWords hundredsToWords)
    {
        _hundredsToWords = hundredsToWords;
    }

    public string ThousandsSplit(int number, bool hasBefore)
    {
        if (number >= 1000000 || number < 0)
            throw new ArgumentException($"ThousandsSplitLT got {number}");

        bool addWhitespace = hasBefore;
        string text = string.Empty;
        int thousands = number / 1000;

        if (thousands > 0)
        {
            text = hasBefore ? " " : string.Empty;
            addWhitespace = true;
            int tensOfThousands = thousands / 10 % 10;
            int hundredsOfThousands = thousands % 1000;

            if (thousands % 10 == 0 || tensOfThousands == 1)
                text += _hundredsToWords.HundredsSplit(hundredsOfThousands, false) + " tūkstančių";
            else if (thousands % 10 == 1)
                text += _hundredsToWords.HundredsSplit(hundredsOfThousands, false) + " tūkstantis";
            else if (thousands > 1)
                text += _hundredsToWords.HundredsSplit(hundredsOfThousands, false) + " tūkstančiai";
        }

        int hundreds = number % 1000;
        text += _hundredsToWords.HundredsSplit(hundreds, addWhitespace);

        return text;
    }
}

public class MillionsToWordsLT : IMillionsToWords
{
    private readonly IHundredsToWords _hundredsToWords;
    private readonly IThousandsToWords _thousandsToWords;

    public MillionsToWordsLT(IHundredsToWords hundredsToWords, IThousandsToWords thousandsToWords)
    {
        _hundredsToWords = hundredsToWords;
        _thousandsToWords = thousandsToWords;
    }

    public string MillionsSplit(int number)
    {
        bool addWhitespace = false;
        string text = string.Empty;
        int millions = number / 1000000;

        // billions not supported
        if (number >= 1000000000 || number < 0)
            throw new ArgumentException($"MillionsToWordsLT got {number}");

        if (millions > 0)
        {
            addWhitespace = true;
            int tensOfMillions = millions / 10 % 10;
            int hundredsOfMillions = millions % 1000;

            if (millions % 10 == 0 || tensOfMillions == 1)
                text += _hundredsToWords.HundredsSplit(hundredsOfMillions, false) + " milijonų";
            else if (millions % 10 == 1)
                text = _hundredsToWords.HundredsSplit(hundredsOfMillions, false) + " milijonas";
            else if (millions > 1)
                text += _hundredsToWords.HundredsSplit(hundredsOfMillions, false) + " milijonai";
        }

        int thousands = number % 1000000;
        text += _thousandsToWords.ThousandsSplit(thousands, addWhitespace);

        return text;
    }
}
