using FluentAssertions;
using System.Reflection;

namespace xUnitTests.Application.MappingProfiles;

public class MappingTestHelper
{
    public enum MapStyle
    {
        UsedAllFrom,
        MappedAllTo
    }

    public static void TestMapp<T1, T2>(T1 from, T2 to, MapStyle style, HashSet<string>? ignoreProps = null)
    {
        to.Should().NotBeNull();
        // Assert
        var fromProps = typeof(T1).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var toProps = typeof(T2).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Func<object?, bool> isDefault = value =>
        {
            if (value == null)
                return true;

            var type = value.GetType();
            if (type.IsValueType)
                return value.Equals(Activator.CreateInstance(type));

            if (type == typeof(string))
                return string.IsNullOrEmpty((string)value);

            return false;
        };

        if (style == MapStyle.MappedAllTo)
        {
            foreach (var toProp in toProps)
            {
                if (ignoreProps != null && ignoreProps.Contains(toProp.Name))
                    continue;

                var fromProp = fromProps.FirstOrDefault(p => p.Name == toProp.Name && p.PropertyType == toProp.PropertyType);
                fromProp.Should().NotBeNull($"Property '{toProp.Name}' should exist in source type.");

                var fromValue = fromProp!.GetValue(from);
                var toValue = toProp.GetValue(to);

                fromValue.Should().NotBeNull();
                toValue.Should().NotBeNull();

                var fromIsDefault = isDefault(fromValue);
                var toIsDefault = isDefault(toValue);

                fromIsDefault.Should().BeFalse($"Property '{toProp.Name}' in source should not be default.");
                toIsDefault.Should().BeFalse($"Property '{toProp.Name}' in target should not be default.");
                toValue.Should().BeEquivalentTo(fromValue, $"Property '{toProp.Name}' should have equivalent value.");
            }
        }
        else
        {
            foreach (var fromProp in fromProps)
            {
                if (ignoreProps != null && ignoreProps.Contains(fromProp.Name))
                    continue;

                var toProp = toProps.FirstOrDefault(p => p.Name == fromProp.Name && p.PropertyType == fromProp.PropertyType);
                toProp.Should().NotBeNull($"Property '{fromProp.Name}' should exist in target type.");

                var fromValue = fromProp.GetValue(from);
                var toValue = toProp!.GetValue(to);

                fromValue.Should().NotBeNull();
                toValue.Should().NotBeNull();

                var fromIsDefault = isDefault(fromValue);
                var toIsDefault = isDefault(toValue);

                fromIsDefault.Should().BeFalse($"Property '{toProp.Name}' in source should not be default.");
                toIsDefault.Should().BeFalse($"Property '{toProp.Name}' in target should not be default.");
                fromValue.Should().BeEquivalentTo(toValue, $"Property '{fromProp.Name}' should have equivalent value.");
            }
        }
    }
}
