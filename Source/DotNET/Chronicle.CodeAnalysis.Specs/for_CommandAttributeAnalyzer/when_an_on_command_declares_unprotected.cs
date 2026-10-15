// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_an_on_command_declares_unprotected
{
    [Fact] public Task should_name_the_rollout_and_opt_out() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; using Cratis.Arc.Chronicle.ReadModels; [ReadProtection(ReadProtectionMode.On), Unprotected] public class Command { public void Handle() { } }",
        new ExpectedDiagnostic("ARCCHR0029", DiagnosticSeverity.Error, "Command 'Command' declares both ReadProtection(On) and Unprotected."));
}
