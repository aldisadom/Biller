using System.Xml;

namespace Application.Helpers.NumberToWords;

public class NumberToWordsLT : INumberToWords
{
    private readonly IOnesToWords _onesToWords;
    private readonly ITensToWords _tensToWords;
    private readonly IHundredsToWords _hundredsToWords;
    private readonly IThousandsToWords _thousandsToWords;
    private readonly IMillionsToWords _millionsToWords;

    public NumberToWordsLT()
    {
        _onesToWords = new OnesToWordsLT(_tensToWords);
        _tensToWords = new TensToWordsLT(_onesToWords, _hundredsToWords);
        _hundredsToWords = new HundredsToWordsLT(_onesToWords, _tensToWords, _thousandsToWords);
        _thousandsToWords = new ThousandsToWordsLT(_hundredsToWords, _millionsToWords);
        _millionsToWords = new MillionsToWordsLT(_hundredsToWords, _thousandsToWords);
    }   

    public string OnesSplit(int number)
    {
        return _onesToWords.OnesSplit(number);
    }

    public string TensSplit(int number)
    {
        return _tensToWords.TensSplit(number);
    }

    public string HundredsSplit(int number)
    {
        return _hundredsToWords.HundredsSplit(number);
    }
    public string ThousandsSplit(int number)
    {
        return _thousandsToWords.ThousandsSplit(number);
    }
    public string MillionsSplit(int number)
    {
        return _millionsToWords.MillionsSplit(number);
    }
}

public class OnesToWordsLT : IOnesToWords
{
    private readonly ITensToWords _tensToWordsLT;

    public OnesToWordsLT(ITensToWords tensToWords)
    {
        _tensToWordsLT = tensToWords;
    }

    public string OnesSplit(int number)
    {
        if (number >= 10)
            return _tensToWordsLT.TensSplit(number);

        string text = number switch
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
    private readonly IHundredsToWords _hundredsToWords;

    public TensToWordsLT(IOnesToWords onesToWords, IHundredsToWords hundredsToWords)
    {
        _onesToWordsLT = onesToWords;
        _hundredsToWords = hundredsToWords;
    }

    public string TensSplit(int number)
    {
        if (number >= 100)
            return _hundredsToWords.HundredsSplit(number);

        string text = string.Empty;
        int tens = number / 10;

        if (tens > 0)
        {
            if (tens == 1)
                text = (number%10) switch
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
                    9 => "devyniolika"
                };
            if (tens == 2)                          
                text = "tvidešimt";
            else if (tens == 3)
                text = "trisdešimt";
            else
                text = _onesToWordsLT.OnesSplit(number / 10) + "asdešimt";
        }

        int ones = number % 100;
        text += _onesToWordsLT.OnesSplit(ones);

        return text;   
    }    
}

public class HundredsToWordsLT : IHundredsToWords
{
    private readonly IOnesToWords _onesToWordsLT;
    private readonly ITensToWords _tensToWordsLT;
    private readonly IThousandsToWords _thousandsToWordsLT;

    public HundredsToWordsLT(IOnesToWords onesToWords, ITensToWords tensToWords, IThousandsToWords thousandsToWordsLT)
    {
        _onesToWordsLT = onesToWords;
        _tensToWordsLT = tensToWords;
        _thousandsToWordsLT = thousandsToWordsLT;
    }

    public string HundredsSplit(int number)
    {        
        if (number >= 1000)
            return _thousandsToWordsLT.ThousandsSplit(number);

        string text = string.Empty;
        int hundreds = number / 100;

        if (hundreds > 0)
        {
            text = _onesToWordsLT.OnesSplit(number / 100);
            if (number / 100 == 1)
                text += " šimtas";
            else
                text += " šimtai";
        }

        int tens = number % 100;
        text += _tensToWordsLT.TensSplit(tens);

        return text;        
    }
}

public class ThousandsToWordsLT : IThousandsToWords
{
    private readonly IHundredsToWords _hundredsToWords;
    private readonly IMillionsToWords _millionsToWords;

    public ThousandsToWordsLT(IHundredsToWords hundredsToWords, IMillionsToWords millionsToWords)
    {
        _hundredsToWords = hundredsToWords;
        _millionsToWords = millionsToWords;
    }

    public string ThousandsSplit(int number)
    {
        if (number >= 1000000)
            return _millionsToWords.MillionsSplit(number);

        string text = string.Empty;
        int thousands = number / 1000;

        if (thousands > 0)
        {
            if (thousands % 100 > 10 && thousands % 100 < 20 || thousands > 0 && thousands % 10 == 0)
                text = _hundredsToWords.HundredsSplit(thousands) + " tūkstančių ";
            else if (thousands % 10 == 1)
                text = _hundredsToWords.HundredsSplit(thousands) + " tūkstantis ";
            else if (thousands > 1)
                text = _hundredsToWords.HundredsSplit(thousands) + " tūkstančiai ";
        }
        
        int hundreds = number % 1000;
        text += _hundredsToWords.HundredsSplit(hundreds);

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
        string text = string.Empty;
        int millions = number / 1000000;

        // billions not supported
        if (number >= 1000000000)
            throw new ArgumentException($"MillionsToWordsLT got {number}");

        if (millions > 0)
        {
            if (millions % 100 > 10 && millions % 100 < 20 || millions > 0 && millions % 10 == 0)
                text = _hundredsToWords.HundredsSplit(millions) + " milijonų ";
            else if (millions % 10 == 1)
                text = _hundredsToWords.HundredsSplit(millions) + " milijonas ";
            else if (millions > 1)
                text = _hundredsToWords.HundredsSplit(millions) + " milijonai ";
        }

        int thousands = number % 1000000;
        text += _thousandsToWords.ThousandsSplit(thousands);

        return text;
    }
}
