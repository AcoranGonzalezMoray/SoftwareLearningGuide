using AwesomeAssertions;
using AwesomeAssertions.Equivalency;

namespace SoftwareLearningGuide.Helper.Test.Extensions;

public static class AwesomeAssertionExtensions {
    public static EquivalencyOptions<T> WithTolerance<T>(this EquivalencyOptions<T> options) =>
        options
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1)))
            .WhenTypeIs<DateTime>();
}
