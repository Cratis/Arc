// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_lookup_property_has_a_different_key_type
{
    [Theory]
    [InlineData("int", "string")]
    [InlineData("Identifier", "string")]
    [InlineData("string", "Identifier")]
    [InlineData("Source", "EventSourceId")]
    [InlineData("EventSourceId", "Source")]
    [InlineData("Guid", "string")]
    [InlineData("string", "Guid")]
    [InlineData("GuidIdentifier", "Guid")]
    [InlineData("Guid", "GuidIdentifier")]
    [InlineData("object", "Guid")]
    public Task should_accept_the_runtime_key_conversion(string propertyType, string modelKeyType) => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync($$"""
        using System;
        using Cratis.Arc.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        public class Command
        {
            public {{propertyType}} Key { get; } = default!;
            public void Handle([LookUpBy(nameof(Key))] Model model) { }
        }
        public class Model { public {{modelKeyType}} Id { get; } = default!; }
        public record Identifier(string Value) : ConceptAs<string>(Value);
        public record GuidIdentifier(Guid Value) : ConceptAs<Guid>(Value);
        public record Source(string Value) : EventSourceId(Value);
        """);
}
