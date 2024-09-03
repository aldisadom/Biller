namespace Application.Helpers.NumberToWords;

public interface INumberToWords : IMillionsToWords, IThousandsToWords, IHundredsToWords, ITensToWords, IOnesToWords
{

}

public interface IOnesToWords
{
    public string OnesSplit(int number);
}

public interface ITensToWords
{
    public string TensSplit(int number);
}

public interface IHundredsToWords
{
    public string HundredsSplit(int number);
}

public interface IThousandsToWords
{
    public string ThousandsSplit(int number);
}

public interface IMillionsToWords
{
    public string MillionsSplit(int number);
}
