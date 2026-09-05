// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions.when_resolving_through_di;

public class and_the_instance_is_absent : Specification
{
    ServiceProvider _rootProvider;
    IServiceScope _scope;
    object? _resolved;

    void Establish()
    {
        var commandContext = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new CommandContextValues());

        _rootProvider = new ServiceCollection()
            .AddScoped(_ => commandContext)
            .AddReadModelsForCommand(new a_read_model_resolver([typeof(FirstReadModel)]))
            .BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
    }

    void Because() => _resolved = _scope.ServiceProvider.GetService(typeof(FirstReadModel));

    [Fact] void should_resolve_to_null() => _resolved.ShouldBeNull();

    void Destroy()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
    }
}
