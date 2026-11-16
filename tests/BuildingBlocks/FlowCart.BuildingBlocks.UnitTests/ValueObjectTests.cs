using FlowCart.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace FlowCart.BuildingBlocks.UnitTests;

public class ValueObjectTests
{
    private class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void TwoValueObjects_WithIdenticalValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
        (money1 != money2).Should().BeFalse();
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void TwoValueObjects_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(200.00m, "USD");
        var money3 = new Money(100.50m, "EUR");

        // Act & Assert
        money1.Should().NotBe(money2);
        money1.Should().NotBe(money3);
        (money1 == money2).Should().BeFalse();
        (money1 == money3).Should().BeFalse();
    }
}
