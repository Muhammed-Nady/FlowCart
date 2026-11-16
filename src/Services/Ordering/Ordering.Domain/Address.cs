using FlowCart.BuildingBlocks.Domain;

namespace Ordering.Domain;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string ZipCode { get; }

    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        Country = string.Empty;
        ZipCode = string.Empty;
    }

    public Address(string street, string city, string state, string country, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new OrderingDomainException("Street cannot be empty.");
        if (string.IsNullOrWhiteSpace(city)) throw new OrderingDomainException("City cannot be empty.");
        if (string.IsNullOrWhiteSpace(country)) throw new OrderingDomainException("Country cannot be empty.");
        if (string.IsNullOrWhiteSpace(zipCode)) throw new OrderingDomainException("ZipCode cannot be empty.");

        Street = street.Trim();
        City = city.Trim();
        State = state?.Trim() ?? string.Empty;
        Country = country.Trim();
        ZipCode = zipCode.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }

    public override string ToString() => $"{Street}, {City}, {State} {ZipCode}, {Country}";
}
