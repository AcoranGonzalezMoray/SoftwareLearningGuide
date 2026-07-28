using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.ValueObjects;

public class OrderIdShould {
    [Test]
    public void Create_GeneratesNewGuid() {
        var id = OrderId.Create();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void Create_MultipleCalls_GenerateDifferentIds() {
        var id1 = OrderId.Create();
        var id2 = OrderId.Create();

        id1.Value.Should().NotBe(id2.Value);
    }

    [Test]
    public void From_ValidGuid_ReturnsSuccess() {
        var guid = Guid.NewGuid();

        var result = OrderId.From(guid);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(guid);
    }

    [Test]
    public void From_EmptyGuid_ReturnsFailure() {
        var result = OrderId.From(Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.IdErrors.OrderIdCannotBeEmpty());
    }

    [Test]
    public void ToString_ReturnsGuidString() {
        var id = OrderId.Create();

        id.ToString().Should().Be(id.Value.ToString());
    }
}

public class CustomerIdShould {
    [Test]
    public void Create_GeneratesNewGuid() {
        var id = CustomerId.Create();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void From_ValidGuid_ReturnsSuccess() {
        var guid = Guid.NewGuid();

        var result = CustomerId.From(guid);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(guid);
    }

    [Test]
    public void From_EmptyGuid_ReturnsFailure() {
        var result = CustomerId.From(Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.IdErrors.CustomerIdCannotBeEmpty());
    }

    [Test]
    public void ToString_ReturnsGuidString() {
        var id = CustomerId.Create();

        id.ToString().Should().Be(id.Value.ToString());
    }
}

public class ProductIdShould {
    [Test]
    public void Create_GeneratesNewGuid() {
        var id = ProductId.Create();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void From_ValidGuid_ReturnsSuccess() {
        var guid = Guid.NewGuid();

        var result = ProductId.From(guid);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(guid);
    }

    [Test]
    public void From_EmptyGuid_ReturnsFailure() {
        var result = ProductId.From(Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.IdErrors.ProductIdCannotBeEmpty());
    }

    [Test]
    public void ToString_ReturnsGuidString() {
        var id = ProductId.Create();

        id.ToString().Should().Be(id.Value.ToString());
    }
}

public class OrderLineIdShould {
    [Test]
    public void Create_GeneratesNewGuid() {
        var id = OrderLineId.Create();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void From_ValidGuid_ReturnsSuccess() {
        var guid = Guid.NewGuid();

        var result = OrderLineId.From(guid);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(guid);
    }

    [Test]
    public void From_EmptyGuid_ReturnsFailure() {
        var result = OrderLineId.From(Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.IdErrors.OrderLineIdCannotBeEmpty());
    }

    [Test]
    public void ToString_ReturnsGuidString() {
        var id = OrderLineId.Create();

        id.ToString().Should().Be(id.Value.ToString());
    }
}
