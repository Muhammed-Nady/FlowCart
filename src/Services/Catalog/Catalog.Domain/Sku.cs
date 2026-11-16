using System.Text.RegularExpressions;
using FlowCart.BuildingBlocks.Domain;

namespace Catalog.Domain;

public sealed class Sku : ValueObject
{
    private static readonly Regex SkuRegex = new("^[A-Z0-9_-]{3,30}$", RegexOptions.Compiled);

    public string Value { get; }

    private Sku()
    {
        Value = string.Empty;
    }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CatalogDomainException("SKU cannot be empty.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!SkuRegex.IsMatch(normalized))
        {
            throw new CatalogDomainException($"Invalid SKU format '{value}'. SKU must be 3-30 uppercase alphanumeric characters, dashes, or underscores.");
        }

        Value = normalized;
    }

    public static Sku Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Sku sku) => sku.Value;
}
