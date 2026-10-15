// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_lookup_property_matches_the_key_type
{
    [Fact] public Task should_accept_the_lookup() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; public class Command { public string Key { get; } = string.Empty; public void Handle([LookUpBy(nameof(Key))] Model model) { } } public class Model { public string Id { get; } = string.Empty; }");
}
