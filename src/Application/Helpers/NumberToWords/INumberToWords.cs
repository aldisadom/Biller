namespace Application.Helpers.NumberToWords;

public interface INumberToWords : IMillionsToWords, IThousandsToWords, IHundredsToWords, ITensToWords, IOnesToWords
{
}

public interface IOnesToWords
{
    public string OnesSplit(int number, bool hasBefore);
}

public interface ITensToWords
{
    public string TensSplit(int number, bool hasBefore);
}

public interface IHundredsToWords
{
    public string HundredsSplit(int number, bool hasBefore);
}

public interface IThousandsToWords
{
    public string ThousandsSplit(int number, bool hasBefore);
}

public interface IMillionsToWords
{
    public string MillionsSplit(int number);
}
