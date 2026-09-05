// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions.when_adding_read_models_from_a_declaring_provider;

public class and_a_fallback_provider_already_claimed_the_read_model : Specification
{
    ServiceProvider _rootProvider;
    IServiceScope _scope;
    FirstReadModel _fromDeclaringProvider;
    object? _resolved;

    void Establish()
    {
        _fromDeclaringProvider = new FirstReadModel("declared");
        var commandContext = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new CommandContextValues());

        _rootProvider = new ServiceCollection()
            .AddScoped(_ => commandContext)
            .AddReadModelsForCommand(new a_read_model_resolver(
                [typeof(FirstReadModel)],
                new Dictionary<Type, object?> { [typeof(FirstReadModel)] = new FirstReadModel("fallback") },
                ReadModelForCommandOwnership.Fallback))
            .AddReadModelsForCommand(new a_read_model_resolver(
                [typeof(FirstReadModel)],
                new Dictionary<Type, object?> { [typeof(FirstReadModel)] = _fromDeclaringProvider }))
            .BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
    }

    void Because() => _resolved = _scope.ServiceProvider.GetService(typeof(FirstReadModel));

    [Fact] void should_take_the_read_model_over_from_the_fallback_provider() => _resolved.ShouldEqual(_fromDeclaringProvider);

    void Destroy()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
    }
}
