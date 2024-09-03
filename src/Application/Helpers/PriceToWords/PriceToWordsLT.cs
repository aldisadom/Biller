using Application.Helpers.PriceToWords;

namespace Application.Helpers.NumberToWords;

public class PriceToWordsLT : IPriceToWords
{
    private readonly INumberToWords _numberToWords;

    public PriceToWordsLT(INumberToWords numberToWords)
    {
        _numberToWords = numberToWords;
    }

    public string Decode(decimal price)
    {
        string totalPriceInWords = string.Empty;
        string tmpText = string.Empty;

        totalPriceInWords += $"€ {(int)(price * 100 % 100)} ct.";

        int tmpPrice = (int)(price % 1000);

        if (tmpPrice >= 1)
            tmpText = _numberToWords.HundredsSplit(tmpPrice) + " ";

        totalPriceInWords = tmpText + totalPriceInWords;
        price /= 1000;
        tmpPrice = (int)(price % 1000);
        tmpText = string.Empty;

        _numberToWords.ThousandsSplit(tmpPrice);

        totalPriceInWords = tmpText + totalPriceInWords;
        price /= 1000;
        tmpPrice = (int)(price % 1000);
        tmpText = string.Empty;

        if (tmpPrice % 100 > 10 && tmpPrice % 100 < 20 || tmpPrice > 0 && tmpPrice % 10 == 0)
            tmpText = _numberToWords.HundredsSplit(tmpPrice) + " milijonų ";
        else if (tmpPrice % 10 == 1)
            tmpText = _numberToWords.HundredsSplit(tmpPrice) + " milijonas ";
        else if (tmpPrice > 1)
            tmpText = _numberToWords.HundredsSplit(tmpPrice) + " milijonai ";

        totalPriceInWords = tmpText + totalPriceInWords;

        return totalPriceInWords;
    }
}
