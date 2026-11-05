// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausationScope.when_beginning;

public class without_a_capability_caller : given.a_capability_causation_scope
{
    int _inside;
    int _after;
    async Task Because()
    {
        _scope.Begin(_context);
        await RunInner();
        _after = _manager.GetCurrentChain().Count(_ => _.Type == CapabilityCausation.Type);
        await _scope.Complete(_context, CommandResult.Success(_context.CorrelationId));
    }
    async Task RunInner()
    {
        _callers.Current.Returns((CapabilityCaller?)null);
        _scope.Begin(_context);
        _inside = _manager.GetCurrentChain().Count(_ => _.Type == CapabilityCausation.Type);
        await _scope.Complete(_context, CommandResult.Success(_context.CorrelationId));
    }
    [Fact] void should_not_add_a_scope() => _inside.ShouldEqual(1);
    [Fact] void should_not_dispose_outer_scope() => _after.ShouldEqual(1);
}
