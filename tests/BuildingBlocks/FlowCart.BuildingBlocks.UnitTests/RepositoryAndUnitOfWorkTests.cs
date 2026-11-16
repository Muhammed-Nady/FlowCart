using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using FlowCart.BuildingBlocks.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FlowCart.BuildingBlocks.UnitTests;

public class RepositoryAndUnitOfWorkTests
{
    public class TestProduct : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;
        public decimal Price { get; private set; }

        private TestProduct() { }

        public TestProduct(Guid id, string name, decimal price) : base(id)
        {
            Name = name;
            Price = price;
        }

        public void UpdatePrice(decimal newPrice)
        {
            Price = newPrice;
            AddDomainEvent(new PriceChangedDomainEvent(Id, newPrice));
        }
    }

    public record PriceChangedDomainEvent(Guid ProductId, decimal NewPrice) : DomainEvent;

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestProduct> Products => Set<TestProduct>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestProduct>().HasKey(p => p.Id);
            modelBuilder.Entity<TestProduct>().Ignore(p => p.DomainEvents);
        }
    }

    private static TestDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task Repository_AddAndGetById_ShouldPersistAndRetrieveEntity()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repository = new Repository<TestProduct, Guid, TestDbContext>(context);
        var unitOfWork = new UnitOfWork<TestDbContext>(context);

        var product = new TestProduct(Guid.NewGuid(), "Mechanical Keyboard", 129.99m);

        // Act
        await repository.AddAsync(product);
        await unitOfWork.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(product.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Mechanical Keyboard");
        retrieved.Price.Should().Be(129.99m);
    }

    [Fact]
    public async Task Repository_GetPagedAsync_ShouldReturnCorrectPageAndCount()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repository = new Repository<TestProduct, Guid, TestDbContext>(context);
        var unitOfWork = new UnitOfWork<TestDbContext>(context);

        for (int i = 1; i <= 10; i++)
        {
            await repository.AddAsync(new TestProduct(Guid.NewGuid(), $"Product {i}", i * 10m));
        }
        await unitOfWork.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(
            pageNumber: 2,
            pageSize: 3,
            orderBy: q => q.OrderBy(p => p.Price));

        // Assert
        totalCount.Should().Be(10);
        items.Should().HaveCount(3);
        items[0].Price.Should().Be(40m);
        items[1].Price.Should().Be(50m);
        items[2].Price.Should().Be(60m);
    }

    [Fact]
    public async Task UnitOfWork_ShouldDispatchDomainEvents_AndClearThemAfterSaving()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        var unitOfWork = new UnitOfWork<TestDbContext>(context, dispatcherMock.Object);
        var repository = new Repository<TestProduct, Guid, TestDbContext>(context);

        var product = new TestProduct(Guid.NewGuid(), "Gaming Mouse", 79.99m);
        product.UpdatePrice(89.99m);
        await repository.AddAsync(product);

        // Act
        await unitOfWork.SaveChangesAsync();

        // Assert
        dispatcherMock.Verify(d => d.DispatchEventsAsync(
            It.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is PriceChangedDomainEvent)),
            It.IsAny<CancellationToken>()), Times.Once);

        product.DomainEvents.Should().BeEmpty();
    }
}
