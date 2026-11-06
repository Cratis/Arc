// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausationScope;

public class when_completing : given.a_capability_causation_scope
{
    int _remaining;
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_context.CorrelationId));
        _remaining = _manager.GetCurrentChain().Count(_ => _.Type == CapabilityCausation.Type);
    }
    [Fact] void should_dispose_the_scope() => _remaining.ShouldEqual(0);
}
