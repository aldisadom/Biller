using Application.Helpers.NumberToWords;

namespace Application.Helpers.PriceToWords;

public class PriceToWordsLT : IPriceToWords
{
    private readonly INumberToWords _numberToWords;

    public PriceToWordsLT(INumberToWords numberToWords)
    {
        _numberToWords = numberToWords;
    }

    public string Decode(decimal price)
    {
        string euros = _numberToWords.MillionsSplit((int)price);
        string cents = ((int)(price * 100 % 100)).ToString();
        return $"{euros} € {cents} ct.";
    }
}
