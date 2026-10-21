// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_missing_lookup_has_no_candidate
{
    [Fact] public Task should_not_offer_a_nameof_fix() => CodeFixVerifier<CommandAttributeAnalyzer, UseNameofForLookUpByCodeFixProvider>.VerifyNoCodeFixAsync(
        "using Cratis.Arc.Commands; public class Command { public void Handle([LookUpBy(\"Missing\")] Model model) { } } public class Model;",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "parameter 'model'", "missing public instance property 'Missing'"));
}
