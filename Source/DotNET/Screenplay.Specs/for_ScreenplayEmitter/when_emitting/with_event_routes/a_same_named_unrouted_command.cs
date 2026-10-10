// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_event_routes;

public class a_same_named_unrouted_command : a_routed_model
{
    void Because()
    {
        var model = Model();
        var other = SliceModel.Empty("Library.Authors.Legacy", "Legacy", SliceKind.StateChange) with
        {
            Commands = [_command with { Authoring = null, Concurrency = new(true, null, null, null, []) }]
        };
        _result = _emitter.Emit(model with { Slices = model.Slices.Append(other).ToList() }, _options);
    }

    [Fact] void should_preserve_the_unrouted_concurrency_block() => _result.Source.ShouldContain("concurrency");
    [Fact] void should_state_the_other_commands_route() => _result.Source.ShouldContain("stream Account.Transactions");
    [Fact] void should_keep_the_unrouted_legacy_tolerance() => new ScreenplayVerifier().Verify(_result.Source).UnexpectedBindingErrors().ShouldBeEmpty();
    [Fact] void should_not_report_a_document_binding_failure() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
    [Fact] void should_observe_the_known_concurrency_limit() => new ScreenplayVerifier().Verify(_result.Source).BindingDiagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0271");
}
