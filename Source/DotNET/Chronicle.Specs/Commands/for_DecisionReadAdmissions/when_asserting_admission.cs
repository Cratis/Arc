// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_DecisionReadAdmissions;

public class when_asserting_admission
{
    [Fact]
    public void admitted_decision_reads_pass() =>
        DecisionReadAdmissions.ShouldAdmitDecisionReadsOf(typeof(DecideOnAdmittedShapes), typeof(ReadRefusedShapeUnprotected));

    [Fact]
    public void refused_decision_reads_name_the_command_read_model_and_reason()
    {
        var error = Assert.Throws<DecisionReadsAreRefused>(() => DecisionReadAdmissions.ShouldAdmitDecisionReadsOf(typeof(DecideOnRefusedShapes)));
        Assert.Contains($"{typeof(DecideOnRefusedShapes).FullName}: DecisionRead<{typeof(AdmissionShelf).FullName}> is refused (Hierarchy)", error.Message);
    }
}
