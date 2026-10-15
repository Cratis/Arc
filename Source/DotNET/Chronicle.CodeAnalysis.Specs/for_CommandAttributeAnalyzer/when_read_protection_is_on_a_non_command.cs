// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_read_protection_is_on_a_non_command
{
    [Fact] public Task should_explain_the_inert_declaration() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; [ReadProtection(ReadProtectionMode.On)] public sealed class Context;",
        new ExpectedDiagnostic("ARCCHR0032", DiagnosticSeverity.Info, "Context", "ReadProtection applies only to commands or assemblies"));
}
