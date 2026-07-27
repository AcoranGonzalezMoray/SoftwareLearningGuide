using NUnit.Framework;

namespace SoftwareLearningGuide.Helper.Test.Fixtures;

[SetUpFixture]
public class TestSqlServerTestContainerFixture {
    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        await SqlServerTestContainer.StartContainer();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await SqlServerTestContainer.StopContainer();
    }
}