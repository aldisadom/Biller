namespace xUnitTests.Helpers;

public static class NullChecker
{
    public static List<string> GetNullOrEmptyProperties(object exampleValues)
    {
        return exampleValues.GetType().GetProperties()
                   .Where(pi => pi.GetValue(obj: exampleValues) == null || (pi.PropertyType == typeof(string) && string.IsNullOrEmpty((string)pi.GetValue(obj: exampleValues)!)))
                   .Select(pi => pi.Name)
                   .ToList();
    }
}
