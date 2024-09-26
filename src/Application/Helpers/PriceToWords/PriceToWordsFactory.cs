using Application.Helpers.NumberToWords;
using Common.Enums;

namespace Application.Helpers.PriceToWords
{
    public interface IPriceToWordsFactory
    {
        IPriceToWords GetConverter(Language languageCode);
    }

    public class PriceToWordsFactory : IPriceToWordsFactory
    {
        public IPriceToWords GetConverter(Language languageCode)
        {
            switch (languageCode)
            {
                case Language.LT:
                    OnesToWordsLT onesToWords = new();
                    TensToWordsLT tensToWords = new(onesToWords);
                    HundredsToWordsLT hundredsToWords = new(onesToWords, tensToWords);
                    ThousandsToWordsLT thousandsToWords = new(hundredsToWords);
                    MillionsToWordsLT millionsToWords = new(hundredsToWords, thousandsToWords);

                    return new PriceToWordsLT(new NumberToWordsLT(onesToWords, tensToWords, hundredsToWords, thousandsToWords, millionsToWords));
                default:
                    throw new NotSupportedException($"Language {languageCode} is not supported");
            }
        }
    }
}
