using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;

namespace xUnitTests.Helpers;

public class AutoDataWithDateOnlyAttribute : AutoDataAttribute
{
    public AutoDataWithDateOnlyAttribute() : base(CreateFixture) { }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new DateOnlySpecimenBuilder());
        return fixture;
    }
}

internal class DateOnlySpecimenBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(DateOnly))
            return new NoSpecimen();

        var year = _random.Next(2020, 2030);
        var month = _random.Next(1, 13);
        var day = _random.Next(1, DateTime.DaysInMonth(year, month) + 1);
        return new DateOnly(year, month, day);
    }
}