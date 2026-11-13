// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_StateAwareEmissionGuard;

public class when_guarding_emissions : Specification
{
    public record Plan(bool Enabled);

    public class PlanGuard : StateAwareEmissionGuard<Plan>
    {
        public Func<object, Plan?> Lookup = _ => null;
        public bool KeyThrows;
        public object? Key = "asset";
        public List<StateDenialReason> Denials = [];

        protected override object? GetKey(ObservableQueryEmissionContext context) => KeyThrows ? throw new InvalidOperationException() : Key;

        protected override ValueTask<Plan?> Read(object key, CancellationToken cancellationToken) => ValueTask.FromResult(Lookup(key));

        protected override bool Decide(ClaimsPrincipal? principal, Plan state) => state.Enabled;

        protected override void OnDenied(ObservableQueryEmissionContext context, StateDenialReason reason, Exception? exception) => Denials.Add(reason);
    }

    PlanGuard _guard;
    ObservableQueryEmissionContext _context;
    ObservableQueryEmissionVerdict _enabled;
    ObservableQueryEmissionVerdict _downgraded;
    ObservableQueryEmissionVerdict _missing;
    ObservableQueryEmissionVerdict _failing;
    ObservableQueryEmissionVerdict _noKey;

    void Establish()
    {
        _guard = new();
        _context = new("Q", new QueryArguments(), null, CorrelationId.New(), null!, false, CancellationToken.None);
    }

    async Task Because()
    {
        _guard.Lookup = _ => new Plan(true);
        _enabled = await _guard.Guard(_context);
        _guard.Lookup = _ => new Plan(false);
        _downgraded = await _guard.Guard(_context);
        _guard.Lookup = _ => null;
        _missing = await _guard.Guard(_context);
        _guard.Lookup = _ => throw new InvalidOperationException("store down");
        _failing = await _guard.Guard(_context);
        _guard.Key = null;
        _noKey = await _guard.Guard(_context);
    }

    [Fact] void should_allow_while_the_plan_is_enabled() => _enabled.ShouldEqual(ObservableQueryEmissionVerdict.Allow);
    [Fact] void should_end_the_subscription_when_the_plan_is_withdrawn() => _downgraded.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_end_the_subscription_when_no_state_is_recorded() => _missing.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_fail_closed_when_the_read_fails() => _failing.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_end_the_subscription_when_no_key_is_named() => _noKey.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_say_why() => _guard.Denials.ShouldEqual([StateDenialReason.Rejected, StateDenialReason.NotFound, StateDenialReason.ReadFailed, StateDenialReason.NoKey]);
}
