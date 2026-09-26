// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class with_an_already_captured_scope : given.all_dependencies
{
    ObservableQueryEmissionVerdict _verdict;

    void Establish()
    {
        var cycle = new CyclicScope();
        cycle.Self = cycle;
        _context = _context with
        {
            SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(
                new List<string> { "captured" }, new ArcOptions().JsonSerializerOptions),
            SubscriptionScope = cycle
        };
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
        _first.OnGuard = context => ((List<string>)context.SubscriptionScope!)[0] = "mutated";
    }

    async Task Because() => _verdict = await _guards.Guard(_context);

    [Fact] void should_not_reserialize_the_live_value() => _verdict.ShouldEqual(ObservableQueryEmissionVerdict.Allow);
    [Fact] void should_give_the_first_guard_the_captured_scope() => ((List<string>)_first.Calls.Single().SubscriptionScope!)[0].ShouldEqual("mutated");
    [Fact] void should_give_the_second_guard_an_independent_copy() => ((List<string>)_second.Calls.Single().SubscriptionScope!)[0].ShouldEqual("captured");

    public class CyclicScope
    {
        public CyclicScope? Self { get; set; }
    }
}
