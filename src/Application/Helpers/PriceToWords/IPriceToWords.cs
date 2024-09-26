namespace Application.Helpers.PriceToWords;

public interface IPriceToWords
{
    public string Decode(decimal price);
}
