using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.ValueObjects;

public class EmailShould {
    [Test]
    public void Create_ValidEmail_ReturnsSuccess() {
        var result = Email.Create("test@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("test@example.com");
    }

    [Test]
    public void Create_EmptyEmail_ReturnsFailure() {
        var result = Email.Create("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.CannotBeEmpty());
    }

    [Test]
    public void Create_WhitespaceEmail_ReturnsFailure() {
        var result = Email.Create("   ");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.CannotBeEmpty());
    }

    [Test]
    public void Create_NullEmail_ReturnsFailure() {
        var result = Email.Create(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.CannotBeEmpty());
    }

    [Test]
    public void Create_InvalidFormat_ReturnsFailure() {
        var result = Email.Create("not-an-email");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.InvalidFormat("not-an-email"));
    }

    [Test]
    public void Create_MissingAtSign_ReturnsFailure() {
        var result = Email.Create("testexample.com");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.InvalidFormat("testexample.com"));
    }

    [Test]
    public void Create_MissingDomain_ReturnsFailure() {
        var result = Email.Create("test@");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.InvalidFormat("test@"));
    }

    [Test]
    public void Create_MissingTld_ReturnsFailure() {
        var result = Email.Create("test@example");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.InvalidFormat("test@example"));
    }

    [Test]
    public void Create_UpperCase_ConvertsToLower() {
        var result = Email.Create("TEST@EXAMPLE.COM");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("test@example.com");
    }

    [Test]
    public void Create_TrimsWhitespace() {
        var result = Email.Create("  test@example.com  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("test@example.com");
    }

    [Test]
    public void ToString_ReturnsValue() {
        var result = Email.Create("test@example.com");

        result.Value!.ToString().Should().Be("test@example.com");
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual() {
        var a = Email.Create("test@example.com").Value!;
        var b = Email.Create("test@example.com").Value!;

        a.Should().Be(b);
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual() {
        var a = Email.Create("test@example.com").Value!;
        var b = Email.Create("other@example.com").Value!;

        a.Should().NotBe(b);
    }

    [TestCase("user@domain.co")]
    [TestCase("user.name@domain.com")]
    [TestCase("user+tag@domain.com")]
    [TestCase("user123@domain.org")]
    public void Create_ValidFormats_ReturnsSuccess(string email) {
        var result = Email.Create(email);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Create_ExceedsMaxLength_ReturnsFailure() {
        var longLocalPart = new string('a', 250);
        var email = $"{longLocalPart}@b.com";

        var result = Email.Create(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.EmailErrors.CannotExceedMaxLength(254));
    }
}
