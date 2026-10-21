// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_unprotected_is_on_a_non_read_model_parameter
{
    [Fact] public Task should_name_the_inert_parameter() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Chronicle.ReadModels; public class Command { public void Handle([Unprotected] string context) { } }",
        new ExpectedDiagnostic("ARCCHR0032", DiagnosticSeverity.Info, "context", "Unprotected has no effect on a non-read-model parameter"));
}
