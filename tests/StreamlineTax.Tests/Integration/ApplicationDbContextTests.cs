using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Infrastructure.Persistence;

namespace StreamlineTax.Tests.Integration;

public class ApplicationDbContextTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void CanCreateDatabase()
    {
        using var context = CreateInMemoryContext();
        context.Database.EnsureCreated();
        context.Database.Should().NotBeNull();
    }

    [Fact]
    public void CanAddTransaction()
    {
        using var context = CreateInMemoryContext();
        context.Database.EnsureCreated();

        var userId = Guid.NewGuid();
        var user = new Domain.Entities.AppUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            Name = "Test User"
        };
        context.Users.Add(user);
        context.SaveChanges();

        var transaction = new Domain.Entities.Transaction
        {
            UserId = userId,
            Amount = 100.50m,
            Description = "Test transaction",
            TransactionDate = DateTime.UtcNow,
            Category = Domain.Enums.TransactionCategory.Salary
        };
        context.Transactions.Add(transaction);
        context.SaveChanges();

        context.Transactions.Should().HaveCount(1);
        context.Transactions.First().Amount.Should().Be(100.50m);
    }

    [Fact]
    public void CanAddTaxPeriod()
    {
        using var context = CreateInMemoryContext();
        context.Database.EnsureCreated();

        var userId = Guid.NewGuid();
        var user = new Domain.Entities.AppUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            Name = "Test User"
        };
        context.Users.Add(user);

        var taxAccount = new Domain.Entities.TaxAccount { UserId = userId };
        context.TaxAccounts.Add(taxAccount);
        context.SaveChanges();

        var period = new Domain.Entities.TaxPeriod
        {
            TaxAccountId = taxAccount.Id,
            Name = "2026 Q1",
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc)
        };
        context.TaxPeriods.Add(period);
        context.SaveChanges();

        context.TaxPeriods.Should().HaveCount(1);
        context.TaxPeriods.First().Name.Should().Be("2026 Q1");
    }

    [Fact]
    public void Transaction_CascadeDelete_WhenUserDeleted()
    {
        using var context = CreateInMemoryContext();
        context.Database.EnsureCreated();

        var userId = Guid.NewGuid();
        var user = new Domain.Entities.AppUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            Name = "Test User"
        };
        context.Users.Add(user);
        context.SaveChanges();

        var transaction = new Domain.Entities.Transaction
        {
            UserId = userId,
            Amount = 100m,
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            Category = Domain.Enums.TransactionCategory.Salary
        };
        context.Transactions.Add(transaction);
        context.SaveChanges();

        context.Users.Remove(user);
        context.SaveChanges();

        context.Transactions.Should().BeEmpty();
    }
}
