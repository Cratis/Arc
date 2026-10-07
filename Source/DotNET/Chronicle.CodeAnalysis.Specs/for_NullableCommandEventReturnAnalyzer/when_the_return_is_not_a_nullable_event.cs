// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.NullableCommandEventReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer;

public class when_the_return_is_not_a_nullable_event
{
    [Theory]
    [InlineData("E", "=> new E();")]
    [InlineData("Task<E>", "=> Task.FromResult(new E());")]
    [InlineData("ValueTask<E>", "=> new(new E());")]
    [InlineData("Result<E, ValidationResult>", "=> new E();")]
    [InlineData("OneOf<E, ValidationResult>", "=> new E();")]
    [InlineData("Result<E, Failure?>", "=> new E();")]
    [InlineData("Task<E>?", "=> null;")]
    [InlineData("ICommandOperation?", "=> null;")]
    [InlineData("Operation?", "=> null;")]
    [InlineData("Task<ICommandOperation?>", "=> Task.FromResult<ICommandOperation?>(null);")]
    [InlineData("ValueTask<Operation?>", "=> new((Operation?)null);")]
    [InlineData("Result<Operation?, ValidationResult>", "=> (Operation?)null;")]
    [InlineData("Failure?", "=> null;")]
    [InlineData("string?", "=> null;")]
    [InlineData("Result<Failure?, ValidationResult>", "=> (Failure?)null;")]
    [InlineData("E?[]", "=> new E?[] { null };")]
    public async Task should_not_report_a_diagnostic(string signature, string body) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(signature, body));

    [Fact]
    public async Task should_preserve_the_optional_operation_contract_even_with_event_metadata() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("EventOperation?", "=> null;") + "\n[EventType] public record EventOperation : ICommandOperation;");

    [Theory]
    [InlineData("public E? Handle() => null;")]
    [InlineData("public Task<E?> Handle() => Task.FromResult<E?>(null);")]
    public async Task should_ignore_non_command_types(string method) =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + "\npublic record C { " + method + " }");

    [Theory]
    [InlineData("private E? Handle() => null;")]
    [InlineData("public static E? Handle() => null;")]
    [InlineData("public E? Provide() => null;")]
    public async Task should_ignore_methods_that_are_not_public_instance_handlers(string method) =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + "\n[Command] public record C { " + method + " }");

    [Fact]
    public async Task should_ignore_a_hidden_inherited_nullable_handler() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + @"

            public record Parent { public E? Handle() => null; }
            [Command] public record C : Parent { public new E Handle() => new E(); }
            ");

    [Fact]
    public async Task should_not_unwrap_a_lookalike_result() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("Local.Result<E?, ValidationResult>", "=> new();") + "\nnamespace Local { public record Result<T, TError>; }");

    [Fact]
    public async Task should_not_recognize_a_lookalike_command_attribute() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + @"

            namespace Local
            {
                public class CommandAttribute : Attribute;
                [Command] public record C { public E? Handle() => null; }
            }
            ");

    [Fact]
    public async Task should_not_recognize_a_lookalike_event_attribute() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("Local.NotAnEvent?", "=> null;") + @"

            namespace Local
            {
                public class EventTypeAttribute : Attribute;
                [EventType] public record NotAnEvent;
            }
            ");
}
