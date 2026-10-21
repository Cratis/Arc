// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_an_unprotected_command_enables_read_protection
{
    [Fact]
    public async Task should_resolve_the_declaration() => await AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        [Unprotected, ReadProtection(ReadProtectionMode.Measure)] public class Command { public void Handle() { } }
        """,
        AnalyzerVerifier<CommandAttributeAnalyzer>.Diagnostic("ARCCHR0029").WithSeverity(DiagnosticSeverity.Error));
}
