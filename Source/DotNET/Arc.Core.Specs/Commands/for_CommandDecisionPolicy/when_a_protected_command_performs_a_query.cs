// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_a_protected_command_performs_a_query : Specification
{
    DiscoverableValidators _validators;
    ServiceProvider _provider = null!;
    Exception? _refusedInCommand;
    bool _resolvedInQuery;
    Exception? _refusedInNestedCommand;
    Exception? _refusedAfterQuery;

    void Establish()
    {
        _validators = new DiscoverableValidators(Cratis.Types.Types.Instance);
        _provider = new ServiceCollection().BuildServiceProvider();
    }

    void Because()
    {
        // Leases are AsyncLocal; keep the whole invocation in one flow.
        using var command = CommandDecisionPolicy.Begin(typeof(ProtectedCommand));
        _refusedInCommand = Record.Exception(() => _validators.TryGet(typeof(PageNumber), _provider, out _));
        using (CommandDecisionPolicy.BeginQuery())
        {
            _resolvedInQuery = _validators.TryGet(typeof(PageNumber), _provider, out _);
            using var nested = CommandDecisionPolicy.Begin(typeof(ProtectedCommand));
            _refusedInNestedCommand = Record.Exception(() => _validators.TryGet(typeof(PageNumber), _provider, out _));
        }

        _refusedAfterQuery = Record.Exception(() => _validators.TryGet(typeof(PageNumber), _provider, out _));
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_refuse_validators_of_the_command_itself() => _refusedInCommand.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_run_the_query_paging_validators() => _resolvedInQuery.ShouldBeTrue();
    [Fact] void should_refuse_validators_of_a_protected_command_nested_in_the_query() => _refusedInNestedCommand.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_refuse_validators_again_once_the_query_completes() => _refusedAfterQuery.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
}
