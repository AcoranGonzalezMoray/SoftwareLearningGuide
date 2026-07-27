using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using SoftwareLearningGuide.Infraestructure.Services;

namespace SoftwareLearningGuide.Infraestructure.Test;

public class OutboxWriterShould : EFDatabase<ApplicationDbContext> {
    [Test]
    public void Constructor_NullContext_ThrowsArgumentNullException() {
        var act = () => new OutboxWriter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task WriteAsync_WritesOutboxMessage() {
        var writer = new OutboxWriter(Context);

        await writer.WriteAsync("test-message", CancellationToken.None);
        await Context.SaveChangesAsync();

        var messages = await Context.OutboxMessages.ToListAsync();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be("String");
        messages[0].Content.Should().Contain("test-message");
    }

    [Test]
    public async Task WriteAsync_WithComplexObject_SerializesCorrectly() {
        var writer = new OutboxWriter(Context);
        var data = new { Id = 42, Name = "Test", Price = 99.99 };

        await writer.WriteAsync(data, CancellationToken.None);
        await Context.SaveChangesAsync();

        var messages = await Context.OutboxMessages.ToListAsync();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be("<>f__AnonymousType0`3");
        messages[0].Content.Should().Contain("\"Id\":42");
        messages[0].Content.Should().Contain("\"Name\":\"Test\"");
        messages[0].CreatedOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
