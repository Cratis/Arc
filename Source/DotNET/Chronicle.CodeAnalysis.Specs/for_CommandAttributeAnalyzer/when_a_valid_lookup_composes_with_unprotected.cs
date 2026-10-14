// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_valid_lookup_composes_with_unprotected
{
    [Fact]
    public async Task should_resolve_the_declaration() => await AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        [KeyGuard(KeyGuardMode.On)] public class Command { public string Key { get; } = "key"; public void Handle([Unprotected, LookUpBy(nameof(Key))] Model model) { } } [FromEvent<Changed>] public class Model; [EventType("5908fb8b-80a7-45e8-8157-b6af86425c19")] public record Changed;
        """);
}
