// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausationScope.when_beginning;

public class with_a_capability_caller : given.a_capability_causation_scope
{
    IImmutableList<Causation> _chain;
    async Task Because()
    {
        _scope.Begin(_context);
        _chain = _manager.GetCurrentChain();
        await _scope.Complete(_context, CommandResult.Success(_context.CorrelationId));
    }
    [Fact] void should_begin_capability_scope() => _chain.Single(_ => _.Type == CapabilityCausation.Type).Type.ShouldEqual(CapabilityCausation.Type);
    [Fact] void should_record_caller_properties() => _chain.Single(_ => _.Type == CapabilityCausation.Type).Properties[CapabilityCausation.PersonProperty].ShouldEqual("person");
    [Fact] void should_not_participate_in_commit() => _scope.IsCommitParticipant.ShouldBeFalse();
    [Fact] void should_have_no_commit() => _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.NoCommit);
}
