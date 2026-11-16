using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Infrastructure;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500);

        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("Street")
                .HasMaxLength(200)
                .IsRequired();

            addressBuilder.Property(a => a.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.State)
                .HasColumnName("State")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.Country)
                .HasColumnName("Country")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.ZipCode)
                .HasColumnName("ZipCode")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(o => o.DomainEvents);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Ignore(i => i.DomainEvents);
    }
}
