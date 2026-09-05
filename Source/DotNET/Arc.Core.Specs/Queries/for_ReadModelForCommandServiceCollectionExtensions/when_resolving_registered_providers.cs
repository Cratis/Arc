// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions;

public class when_resolving_registered_providers : Specification
{
    a_read_model_resolver _firstResolver;
    a_read_model_resolver _secondResolver;
    ServiceProvider _serviceProvider;
    ICanResolveReadModelForCommand[] _result;

    void Establish()
    {
        _firstResolver = new([typeof(FirstReadModel)]);
        _secondResolver = new([typeof(SecondReadModel)]);
        _serviceProvider = new ServiceCollection()
            .AddReadModelsForCommand(_firstResolver)
            .AddReadModelsForCommand(_secondResolver)
            .BuildServiceProvider();
    }

    void Because() => _result = _serviceProvider.GetServices<ICanResolveReadModelForCommand>().ToArray();

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_resolve_the_first_provider() => _result.ShouldContain(_firstResolver);
    [Fact] void should_resolve_the_second_provider() => _result.ShouldContain(_secondResolver);
    [Fact] void should_resolve_each_provider_once() => _result.Length.ShouldEqual(2);
}
