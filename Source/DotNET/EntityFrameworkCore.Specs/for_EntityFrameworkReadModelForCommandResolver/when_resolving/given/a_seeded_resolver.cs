// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver.when_resolving.given;

public class a_seeded_resolver : Specification
{
    protected SeedableCustomerDbContext _dbContext;
    protected EntityFrameworkReadModelForCommandResolver _resolver;
    protected IServiceProvider _serviceProvider;
    protected Guid _customerId;
    protected object? _resolved;
    protected Exception _exception;

    void Establish()
    {
        _customerId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<SeedableCustomerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new SeedableCustomerDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _serviceProvider = new ServiceCollection()
            .AddSingleton(_dbContext)
            .BuildServiceProvider();

        _resolver = new EntityFrameworkReadModelForCommandResolver(
            new Dictionary<Type, Type> { [typeof(CustomerReadModel)] = typeof(SeedableCustomerDbContext) });
    }

    void Destroy()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
    }

    protected void SeedCustomer()
    {
        _dbContext.Customers.Add(new CustomerReadModel { Id = _customerId, Name = "Test" });
        _dbContext.SaveChanges();
    }

    protected void ResolveCustomerWithKey(string? resolvedKey) =>
        _resolved = _resolver.Resolve(typeof(CustomerReadModel), CommandContextWithKey(resolvedKey)).GetAwaiter().GetResult();

    protected void CatchResolveCustomerWithKey(string? resolvedKey) =>
        _exception = Catch.Exception(() => _resolver.Resolve(typeof(CustomerReadModel), CommandContextWithKey(resolvedKey)).GetAwaiter().GetResult());

    CommandContext CommandContextWithKey(string? resolvedKey)
    {
        var values = new CommandContextValues();
        if (resolvedKey is not null)
        {
            values[CommandContextKeys.ResolvedKey] = resolvedKey;
        }

        return new CommandContext(CorrelationId.New(), typeof(object), new object(), [], values) with { ServiceProvider = _serviceProvider };
    }
}
