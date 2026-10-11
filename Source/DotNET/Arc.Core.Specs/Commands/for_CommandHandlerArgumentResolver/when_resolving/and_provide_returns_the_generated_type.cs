// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_the_generated_type : given.a_generated_value_resolver
{
    readonly Id _provided = new(Guid.Empty);

    void Establish()
    {
        ProvideReturns(_provided);
        _serviceProvider.GetService(typeof(Id)).Returns(_provided);
    }

    async Task Because() => _resolution = await Resolve();

    [Fact] void should_not_take_the_provided_or_di_value() => _resolution.Arguments.ShouldNotContain(_provided);
    [Fact] void should_not_resolve_the_type_from_di() => _serviceProvider.DidNotReceive().GetService(typeof(Id));
}
