using Application.Helpers.NumberToWords;
using Contracts.Enums;

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

                    return new PriceToWords(new NumberToWordsLT(onesToWords, tensToWords, hundredsToWords, thousandsToWords, millionsToWords));
                case Language.EN:
                    OnesToWordsEN onesToWordsEN = new();
                    TensToWordsEN tensToWordsEN = new(onesToWordsEN);
                    HundredsToWordsEN hundredsToWordsEN = new(onesToWordsEN, tensToWordsEN);
                    ThousandsToWordsEN thousandsToWordsEN = new(hundredsToWordsEN);
                    MillionsToWordsEN millionsToWordsEN = new(hundredsToWordsEN, thousandsToWordsEN);

                    return new PriceToWords(new NumberToWordsEN(onesToWordsEN, tensToWordsEN, hundredsToWordsEN, thousandsToWordsEN, millionsToWordsEN));
                default:
                    throw new NotSupportedException($"Language {languageCode} is not supported");
            }
        }
    }
}
