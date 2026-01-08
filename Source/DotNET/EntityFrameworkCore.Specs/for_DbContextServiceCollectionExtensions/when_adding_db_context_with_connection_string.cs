// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Arc.EntityFrameworkCore.Mapping;
using Cratis.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Cratis.Arc.EntityFrameworkCore.for_DbContextServiceCollectionExtensions;

public class when_adding_db_context_with_connection_string : Specification
{
    IServiceProvider _serviceProvider;
    TestDbContext _dbContext;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ITypes>());
        services.AddDbContextWithConnectionString<TestDbContext>("Data Source=:memory:");

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext();
    }

    [Fact] void should_register_entity_type_registrar() =>
        _serviceProvider.GetService<IEntityTypeRegistrar>().ShouldNotBeNull();

    [Fact] void should_register_concept_evaluatable_expression_filter()
    {
        var filter = _dbContext.GetService<IEvaluatableExpressionFilter>();
        filter.ShouldBeOfExactType<ConceptAsEvaluatableExpressionFilter>();
    }

    [Fact] void should_register_concept_model_customizer()
    {
        var customizer = _dbContext.GetService<IModelCustomizer>();
        customizer.ShouldBeOfExactType<ConceptAsModelCustomizer>();
    }
}
