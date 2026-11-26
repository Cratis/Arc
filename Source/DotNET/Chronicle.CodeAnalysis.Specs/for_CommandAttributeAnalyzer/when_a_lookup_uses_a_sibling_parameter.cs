// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

/// <summary>
/// Verifies that sibling lookup names are not mistaken for command properties.
/// </summary>
public class when_a_lookup_uses_a_sibling_parameter : Specification
{
    Exception? _failure;
    async Task Because() => _failure = await Catch.Exception(() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; public class Command { public void Handle([LookUpBy(nameof(source), nameof(Model.RelatedId))] Model target, Model source) { } } public class Model { public string Id { get; } = string.Empty; public string RelatedId { get; } = string.Empty; }"));
    [Fact] void should_not_require_a_command_property_named_source() => _failure.ShouldBeNull();
}
