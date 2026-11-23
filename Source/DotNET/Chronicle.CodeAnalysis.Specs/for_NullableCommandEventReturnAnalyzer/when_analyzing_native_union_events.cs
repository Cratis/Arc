// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_NullableCommandEventReturnAnalyzer;

public class when_analyzing_native_union_events : Specification
{
    [Theory]
    [InlineData("Choice<E?, Failure>", "new E()")]
    [InlineData("Choice<E, Failure>?", "new Choice<E, Failure>(new E())")]
    [InlineData("Choice<E, Failure>", "default")]
    [InlineData("Choice<E, Failure>", "default(Choice<E, Failure>)")]
    [InlineData("Choice<E, Failure>", "new Choice<E, Failure>()")]
    [InlineData("Choice<E, Failure>", "new()")]
    [InlineData("Task<Choice<E, Failure>>", "Task.FromResult(new Choice<E, Failure>())")]
    [InlineData("(int, Choice<E?, Failure>)", "(0, new E())")]
    [InlineData("OneOf<Choice<E, Failure>, int>", "default(Choice<E, Failure>)")]
    [InlineData("Choice<E, Failure>", "true ? new E() : default(Choice<E, Failure>)")]
    [InlineData("Choice<E, Failure>", "1 switch { 0 => new E(), _ => default(Choice<E, Failure>) }")]
    [InlineData("(int, Choice<E, Failure>)", "(0, default)")]
    [InlineData("ValueTask<Choice<E, Failure>>", "new(default(Choice<E, Failure>))")]
    [InlineData("Task<Choice<E, Failure>>", "Task.FromResult(default(Choice<E, Failure>))")]
    [InlineData("Choice<Choice<E, Failure>, int>", "new Choice<Choice<E, Failure>, int>(default(Choice<E, Failure>))")]
    public Task should_warn_once_on_optional_or_visible_empty_events(string signature, string expression) =>
        Verify<NullableCommandEventReturnAnalyzer>(Source(signature, expression), Warning());

    [Theory]
    [InlineData("Choice<E, Failure>", "new E()")]
    [InlineData("Choice<E, Failure>", "new Choice<E, Failure>(new E())")]
    [InlineData("Choice<E, Failure>", "Factory()")]
    [InlineData("Task<Choice<E, Failure>>", "Task.FromResult<Choice<E, Failure>>(new E())")]
    [InlineData("Choice<int, Failure>", "default")]
    [InlineData("Choice<Operation, Failure>", "default")]
    [InlineData("(string?, Choice<E, Failure>)", "(null, new E())")]
    public Task should_not_infer_empty_events_from_valid_or_opaque_values(string signature, string expression) =>
        Verify<NullableCommandEventReturnAnalyzer>(Source(signature, expression));

    [Fact]
    public Task should_inspect_block_returns() => Verify<NullableCommandEventReturnAnalyzer>(
        Source("Choice<E, Failure>", "new E()").Replace("=> new E();", "{ if (DateTime.UtcNow.Ticks > 0) return new E(); return default; }", StringComparison.Ordinal), Warning());

    [Fact]
    public Task should_not_inspect_opaque_locals_or_nested_local_functions() => Verify<NullableCommandEventReturnAnalyzer>(
        Source("Choice<E, Failure>", "new E()").Replace("=> new E();", "{ Choice<E, Failure> Empty() => default; var value = Empty(); return value; }", StringComparison.Ordinal));

    [Fact]
    public Task should_not_interpret_user_written_conversions() => Verify<NullableCommandEventReturnAnalyzer>(
        Imports + "[EventType] public record E; public union Other(E, int); public union Outcome(E, string) { public static implicit operator Outcome(Other value) => new(new E()); } " +
        "[Command] public record C { public Outcome Handle() => default(Other); }");

    static string Source(string signature, string expression) => Imports + "\n" + Events + "\n" +
        "public record Operation : Cratis.Arc.Commands.ICommandOperation; [Command] public record C { public {|#0:" + signature + "|} Handle() => " + expression +
        "; static Choice<E, Failure> Factory() => new E(); }";
}
#endif
