// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver.when_injecting_through_di;

/// <summary>
/// Exercises the exact seam the command pipeline uses to inject a read model: a scoped resolution by the read model type
/// through the service provider, keyed by the command's resolved key.
/// </summary>
public class and_the_read_model_exists : Specification
{
    SeedableCustomerDbContext _dbContext;
    ServiceProvider _rootProvider;
    IServiceScope _scope;
    Guid _customerId;
    object? _resolved;

    void Establish()
    {
        _customerId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<SeedableCustomerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new SeedableCustomerDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
        _dbContext.Customers.Add(new CustomerReadModel { Id = _customerId, Name = "Test" });
        _dbContext.SaveChanges();

        var values = new CommandContextValues
        {
            [CommandContextKeys.ResolvedKey] = _customerId.ToString()
        };
        var commandContext = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], values);

        _rootProvider = new ServiceCollection()
            .AddSingleton(_dbContext)
            .AddScoped(_ => commandContext)
            .AddReadModelsForCommand(new EntityFrameworkReadModelForCommandResolver(
                new Dictionary<Type, Type> { [typeof(CustomerReadModel)] = typeof(SeedableCustomerDbContext) }))
            .BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
    }

    void Because() => _resolved = _scope.ServiceProvider.GetService(typeof(CustomerReadModel));

    [Fact] void should_inject_the_read_model() => _resolved.ShouldNotBeNull();
    [Fact] void should_inject_the_read_model_with_the_matching_key() => ((CustomerReadModel)_resolved!).Id.ShouldEqual(_customerId);

    void Destroy()
    {
        _dbContext.Database.CloseConnection();
        _scope.Dispose();
        _rootProvider.Dispose();
    }
}
