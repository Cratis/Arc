// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_parameter_overrides_an_unprotected_method
{
    [Fact]
    public async Task should_resolve_the_declaration() => await AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        [ReadProtection(ReadProtectionMode.On)] public class Command { [Unprotected] public void Handle(Model model) { } } public class Model;
        """);
}
