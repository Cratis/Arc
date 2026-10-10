// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_an_omitted_read_model;

public class and_a_production_condition_reads_it : a_command_with_a_required_read_mapping
{
    void Because()
    {
        var command = _command with
        {
            Produces = [new("Registered", new ComparisonCondition("report.Name", ComparisonKind.Equal, new LiteralSource("name")), [new("Name", new PropertyPathSource("Name")), new("PreviousName", new PropertyPathSource("Name"))]) { UsesCommandContext = true }]
        };
        var model = _application with { Slices = _application.Slices.Select(slice => slice with { Commands = [command] }).ToList() };
        _result = _emitter.Emit(model, new ScreenplayOptions { AuthoringOnlyConstructs = true });
    }

    [Fact] void should_withhold_the_entire_conditional_production() => _result.Source.ShouldNotContain("produces Registered");
    [Fact] void should_not_leave_a_dangling_condition() => _result.Source.ShouldNotContain("report.name");
    [Fact] void should_classify_the_unavailable_condition() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.ShouldContain("condition depends on an unavailable read");
    [Fact] void should_bind_without_unexpected_errors() => AssertNoUnexpectedBindingErrors(true);
}
