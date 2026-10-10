// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_an_omitted_read_model;

public class and_its_projection_is_missing : a_command_with_a_required_read_mapping
{
    void Because()
    {
        var model = _application with { Slices = _application.Slices.Select(slice => slice with { Projections = [] }).ToList() };
        _result = _emitter.Emit(model, new ScreenplayOptions { AuthoringOnlyConstructs = true });
    }

    [Fact] void should_withhold_the_entire_production() => _result.Source.ShouldNotContain("produces Registered");
    [Fact] void should_withhold_the_command_route() => _result.Source.ShouldNotContain("stream Account.Transactions");
    [Fact] void should_name_the_required_mapping() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.ShouldContain("required event property 'PreviousName'");
    [Fact] void should_report_no_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_bind_without_unexpected_errors() => AssertNoUnexpectedBindingErrors(true);
}
