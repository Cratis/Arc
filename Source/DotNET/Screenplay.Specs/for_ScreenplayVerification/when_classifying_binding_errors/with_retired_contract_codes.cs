// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerification.when_classifying_binding_errors;

public class with_retired_contract_codes : given.known_admission_diagnostics
{
    void Establish() => _catalog = _catalog.Select(fact => fact with { Retired = true, Reserved = false }).ToArray();

    void Because() => _expectedCount = _diagnostics.Count(item => ExpectedBindingDiagnostics.IsExpected(item.Diagnostic, item.AuthoringOnly, _catalog));

    [Fact] void should_not_exempt_any_known_message_for_retired_codes() => _expectedCount.ShouldEqual(0);
}
