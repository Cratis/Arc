// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_command_declares_both_protection_profiles
{
    [Fact] public Task should_name_both_declarations() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Chronicle.ReadModels; [ProtectedDecision, Unprotected] public class Command { public void Handle() { } }",
        new ExpectedDiagnostic("ARCCHR0029", DiagnosticSeverity.Error, "Command 'Command' declares both ProtectedDecision and Unprotected."));
}
