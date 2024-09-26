namespace IntegrationTests
{
    internal class Program
    {
        private static readonly TimeOnly Midnight = TimeOnly.FromTimeSpan(TimeSpan.Zero);
        private static readonly TimeOnly Noon = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12));

        [Test]
        public async Task IsMorning()
        {
            var time = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10));

            await Assert.That(time).IsAfterOrEqualTo(Midnight)
                .And.IsBefore(Noon);
        }
    }
}
