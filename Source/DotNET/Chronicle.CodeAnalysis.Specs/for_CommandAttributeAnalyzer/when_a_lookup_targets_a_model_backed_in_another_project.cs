// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_lookup_targets_a_model_backed_in_another_project
{
    [Fact]
    public async Task should_leave_validation_to_the_runtime() => await AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands;
        public record Model(string Id);
        public class Command
        {
            public string Key { get; } = "key";
            public void Handle([LookUpBy(nameof(Key))] Model model) { }
        }
        """);
}
