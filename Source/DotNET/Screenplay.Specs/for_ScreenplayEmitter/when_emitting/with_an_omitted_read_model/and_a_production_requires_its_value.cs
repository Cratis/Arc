// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_an_omitted_read_model;

public class and_a_production_requires_its_value : a_command_with_a_required_read_mapping
{
    void Because() => _result = _emitter.Emit(_application, new ScreenplayOptions { AuthoringOnlyConstructs = true });

    [Fact] void should_withhold_the_entire_production() => _result.Source.ShouldNotContain("produces Registered");
    [Fact] void should_keep_the_handler_pointer() => _result.Source.ShouldContain("Register.cs");
    [Fact] void should_withhold_the_command_route() => _result.Source.ShouldNotContain("stream Account.Transactions");
    [Fact] void should_not_leave_an_orphan_source() => _result.Source.ShouldNotContain("eventsource Account");
    [Fact] void should_withhold_the_successful_scenario() => _result.Source.ShouldNotContain("a_successful_registration");
    [Fact] void should_name_the_required_mapping() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.ShouldContain("required event property 'PreviousName'");
    [Fact] void should_classify_the_withheld_production_as_information() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_report_the_withheld_route() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("all event productions were withheld");
    [Fact] void should_report_the_withheld_scenario() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_report_no_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_bind_without_unexpected_errors() => AssertNoUnexpectedBindingErrors(true);
}
