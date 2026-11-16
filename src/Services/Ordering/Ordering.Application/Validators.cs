using FluentValidation;

namespace Ordering.Application;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("ShippingAddress is required.");

        When(x => x.ShippingAddress != null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street).NotEmpty().WithMessage("Street is required.");
            RuleFor(x => x.ShippingAddress.City).NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.ShippingAddress.Country).NotEmpty().WithMessage("Country is required.");
            RuleFor(x => x.ShippingAddress.ZipCode).NotEmpty().WithMessage("ZipCode is required.");
        });

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("An order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("ProductId is required.");
            item.RuleFor(i => i.ProductName).NotEmpty().WithMessage("ProductName is required.");
            item.RuleFor(i => i.ExpectedUnitPrice).GreaterThan(0).WithMessage("UnitPrice must be greater than zero.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Cancellation reason is required.");
    }
}
