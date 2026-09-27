// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class with_an_unrepresentable_public_scope : given.all_dependencies
{
    ObservableQueryEmissionVerdict _verdict;

    void Establish()
    {
        var scope = new CyclicScope();
        scope.Self = scope;
        _context = _context with { SubscriptionScope = scope };
        DiscoverGuards(typeof(FirstGuard));
    }

    async Task Because() => _verdict = await _guards.Guard(_context);

    [Fact] void should_fail_closed() => _verdict.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_not_call_the_guard() => _first.Calls.ShouldBeEmpty();

    public class CyclicScope
    {
        public CyclicScope? Self { get; set; }
    }
}
