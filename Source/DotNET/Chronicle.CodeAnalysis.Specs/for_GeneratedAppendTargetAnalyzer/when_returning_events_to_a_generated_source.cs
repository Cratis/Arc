// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using Verify = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.GeneratedAppendTargetAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_GeneratedAppendTargetAnalyzer;

public class when_returning_events_to_a_generated_source
{
    const string Types = @"
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Concepts;
        public record CartId(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<CartId>
        {
            public static CartId New() => new(GenerateValue.Uuid());
        }
        public class Cart : AggregateRoot;
        ";

    [Fact]
    public async Task should_warn_about_the_command_key_aggregate() => await Verify.VerifyAnalyzerAsync(
        Types + @"
        [Command]
        public record Allocate(EventSourceId Source)
        {
            public EventForEventSourceId Handle(Cart {|#0:cart|}, [GeneratedValue] CartId id) => new(id, new object());
        }",
        Verify.Diagnostic("ARCCHR0022").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("cart", "id"));

    [Theory]
    [InlineData("public Cart Provide() => new();", "new(id, new object())")]
    [InlineData("", "new(Source, new object())")]
    public async Task should_ignore_unproven_or_matching_aggregate_sources(string provide, string response) => await Verify.VerifyAnalyzerAsync(Types + $@"
        [Command]
        public record Allocate(EventSourceId Source)
        {{
            {provide}
            public EventForEventSourceId Handle(Cart cart, [GeneratedValue] CartId id) => {response};
        }}");

    [Fact]
    public async Task should_warn_about_a_direct_append() => await Verify.VerifyAnalyzerAsync(
        Types + @"
        [Command]
        public record Allocate(EventSourceId Source)
        {
            public async System.Threading.Tasks.Task Handle(Cart {|#0:cart|}, IEventSequence sequence, [GeneratedValue] CartId id)
            {
                await sequence.Append(id, new object());
            }
        }",
        Verify.Diagnostic("ARCCHR0022").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("cart", "id"));

    [Fact]
    public async Task should_ignore_a_custom_source_provider() => await Verify.VerifyAnalyzerAsync(Types + @"
        [Command]
        public record Allocate(EventSourceId Source) : ICanProvideEventSourceId
        {
            public EventSourceId GetEventSourceId() => Source;
            public EventForEventSourceId Handle(Cart cart, [GeneratedValue] CartId id) => new(id, new object());
        }");

    [Fact]
    public async Task should_ignore_a_wrapper_that_is_not_returned() => await Verify.VerifyAnalyzerAsync(Types + @"
        [Command]
        public record Allocate(EventSourceId Source)
        {
            public object Handle(Cart cart, [GeneratedValue] CartId id)
            {
                var unused = new EventForEventSourceId(id, new object());
                return new object();
            }
        }");

    [Theory]
    [InlineData("public EventForEventSourceId Handle([GeneratedValue] CartId id) => new(id, new object());")]
    [InlineData("public EventSourceId Handle(Cart cart) => Source;")]
    [InlineData("public EventForEventSourceId Handle(Cart cart, CartId id) => new(id, new object());")]
    [InlineData("public EventSourceId Handle() => Source;")]
    public async Task should_require_both_aggregate_injection_and_an_explicit_generated_target(string handle) => await Verify.VerifyAnalyzerAsync(Types + $@"
        [Command]
        public record Allocate(EventSourceId Source)
        {{
            {handle}
        }}");

    [Fact]
    public async Task should_ignore_a_generated_parameter_that_is_not_an_append_target() => await Verify.VerifyAnalyzerAsync(Types + @"
        [Command]
        public record Allocate(EventSourceId Source)
        {
            public CartId Handle(Cart cart, [GeneratedValue] CartId id) => id;
        }");
}
