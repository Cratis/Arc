// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

namespace Cratis.Arc.Chronicle.Commands.for_DecisionReadAdmissions;

public class when_checking_reducer_admission : Specification
{
    IReadOnlyList<RefusedDecisionRead> _refused;

    void Because() => _refused = DecisionReadAdmissions.FindRefused([typeof(DecideOnReducers)]);

    [Fact] void should_admit_an_unfiltered_dependency_free_reducer() => _refused.Any(_ => _.ReadModelType == typeof(LedgerBalance)).ShouldBeFalse();
    [Fact] void should_refuse_a_filtered_reducer() => _refused.ShouldContain(new RefusedDecisionRead(typeof(DecideOnReducers), typeof(FilteredBalance), DecisionReadRefusalReason.ReducerFilters));
    [Fact] void should_refuse_a_reducer_with_constructor_dependencies() => _refused.ShouldContain(new RefusedDecisionRead(typeof(DecideOnReducers), typeof(DependentBalance), DecisionReadRefusalReason.ReducerDependencies));
    [Fact] void should_report_only_the_two_unsupported_reducers() => _refused.Count.ShouldEqual(2);

    [Command]
    [ProtectedDecision]
    public record DecideOnReducers(EventSourceId EventSourceId)
    {
        public AdmissionProvided Provide(DecisionRead<LedgerBalance> balance) => new();

        public AdmissionDecided Handle(DecisionRead<FilteredBalance> filtered, DecisionRead<DependentBalance> dependent, AdmissionProvided provided) => new();
    }

    public record FilteredBalance(decimal Balance);

    public record DependentBalance(decimal Balance);

    [FilterEventsByTag("admission")]
    public class FilteredBalanceReducer : IReducerFor<FilteredBalance>
    {
        public FilteredBalance Credited(LedgerCredited @event, FilteredBalance? current) => new((current?.Balance ?? 0m) + @event.Amount);
    }

    public class DependentBalanceReducer(IServiceProvider services) : IReducerFor<DependentBalance>
    {
        public DependentBalance Credited(LedgerCredited @event, DependentBalance? current)
        {
            _ = services.GetService(typeof(object));
            return new((current?.Balance ?? 0m) + @event.Amount);
        }
    }
}
