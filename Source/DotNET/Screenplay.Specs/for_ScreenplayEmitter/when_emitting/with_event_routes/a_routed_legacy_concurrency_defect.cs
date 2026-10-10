// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_event_routes;

public class a_routed_legacy_concurrency_defect : a_routed_model
{
    void Establish() => _command = _command with { Concurrency = new(false, "Account", "Transactions", null, []) };

    void Because() => _result = _emitter.Emit(Model(), new ScreenplayOptions { AuthoringOnlyConstructs = true });

    [Fact] void should_reject_routed_legacy_concurrency_in_default_verification() => new ScreenplayVerifier().Verify(_result.Source).UnexpectedBindingErrors().Single().Code.ShouldEqual("PLAY0271");
    [Fact] void should_keep_the_authoring_legacy_tolerance() => new ScreenplayVerifier().Verify(_result.Source).UnexpectedBindingErrors(true).ShouldBeEmpty();
}
