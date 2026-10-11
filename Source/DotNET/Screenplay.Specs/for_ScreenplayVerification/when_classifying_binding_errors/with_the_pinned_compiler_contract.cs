// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerification.when_classifying_binding_errors;

public class with_the_pinned_compiler_contract : given.known_admission_diagnostics
{
    IEnumerable<string> _unsupportedCodes;

    void Because()
    {
        _unsupportedCodes = ExpectedBindingDiagnostics.AcceptedCodes.Except(CompilerContractCatalog.Diagnostics
            .Where(fact => !fact.Retired && !fact.Reserved && fact.Severities.Contains(DiagnosticSeverity.Error))
            .Select(fact => fact.Code));
        _expectedCount = _diagnostics.Count(item => ExpectedBindingDiagnostics.IsExpected(item.Diagnostic, item.AuthoringOnly, _catalog));
    }

    [Fact] void should_require_every_accepted_code_to_be_an_active_error_in_the_pinned_catalog() => _unsupportedCodes.ShouldBeEmpty();
    [Fact] void should_accept_known_messages_for_active_error_codes() => _expectedCount.ShouldEqual(_diagnostics.Count);
}
