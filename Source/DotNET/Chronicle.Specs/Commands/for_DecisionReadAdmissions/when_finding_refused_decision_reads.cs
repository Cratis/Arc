// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_DecisionReadAdmissions;

public class when_finding_refused_decision_reads : Specification
{
    IReadOnlyList<RefusedDecisionRead> _refused;

    void Because() => _refused = DecisionReadAdmissions.FindRefused([typeof(DecideOnAdmittedShapes), typeof(DecideOnRefusedShapes), typeof(ReadRefusedShapeUnprotected)]);

    [Fact] void should_refuse_the_children_projection() => _refused.ShouldContain(new RefusedDecisionRead(typeof(DecideOnRefusedShapes), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_refuse_the_reducer_read_in_provide() => _refused.ShouldContain(new RefusedDecisionRead(typeof(DecideOnRefusedShapes), typeof(LedgerBalance), DecisionReadRefusalReason.Reducer));
    [Fact] void should_report_only_the_refused_reads() => _refused.Count.ShouldEqual(2);
}
