// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_lookup_targets_a_self_reducing_model
{
    [Fact]
    public async Task should_recognize_the_runtime_read_model_shape() => await AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        #nullable enable
        using Cratis.Arc.Commands;
        using Cratis.Chronicle.Events;
        [EventType("5908fb8b-80a7-45e8-8157-b6af86425c19")] public record Changed;
        public record Model
        {
            public static Model Reduce(Changed changed, Model? current) => new();
        }
        public class Command
        {
            public string Key { get; } = "key";
            public void Handle([LookUpBy(nameof(Key))] Model model) { }
        }
        """);
}
