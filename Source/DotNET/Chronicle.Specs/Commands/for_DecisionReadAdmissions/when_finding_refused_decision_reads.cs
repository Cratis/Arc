// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_DecisionReadAdmissions;

public class when_finding_refused_decision_reads : Specification
{
    IReadOnlyList<RefusedDecisionRead> _refused;
    IReadOnlyList<RefusedDecisionRead> _inherited;

    void Because()
    {
        _refused = DecisionReadAdmissions.FindRefused([typeof(DecideOnAdmittedShapes), typeof(DecideOnRefusedShapes), typeof(ReadRefusedShapeUnprotected)]);
        _inherited = DecisionReadAdmissions.FindRefused([typeof(DecideThroughInheritedHandle), typeof(DecideOnEnumeratedRefusedShapes), typeof(DecideWithRefusedShapeInHelper)]);
    }

    [Fact] void should_refuse_the_children_projection() => _refused.ShouldContain(new RefusedDecisionRead(typeof(DecideOnRefusedShapes), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_refuse_the_reducer_read_in_provide() => _refused.ShouldContain(new RefusedDecisionRead(typeof(DecideOnRefusedShapes), typeof(LedgerBalance), DecisionReadRefusalReason.Reducer));
    [Fact] void should_refuse_the_read_of_an_inherited_handle() => _inherited.ShouldContain(new RefusedDecisionRead(typeof(DecideThroughInheritedHandle), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_refuse_the_enumerated_read() => _inherited.ShouldContain(new RefusedDecisionRead(typeof(DecideOnEnumeratedRefusedShapes), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_not_check_methods_that_are_not_injected() => _inherited.Count.ShouldEqual(2);
    [Fact] void should_report_only_the_refused_reads() => _refused.Count.ShouldEqual(2);
}
