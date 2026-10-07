// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.NullableCommandEventReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer;

public class when_returning_a_nullable_event
{
    [Theory]
    [InlineData("E?", "=> null;")]
    [InlineData("E?", "=> new E();")]
    [InlineData("Task<E?>", "=> Task.FromResult<E?>(null);")]
    [InlineData("ValueTask<E?>", "=> new((E?)null);")]
    [InlineData("Result<E?, ValidationResult>", "=> (E?)null;")]
    [InlineData("OneOf<E?, ValidationResult>", "=> (E?)null;")]
    [InlineData("OneOf<Failure, E?>", "=> (E?)null;")]
    [InlineData("Task<Result<E?, ValidationResult>>", "=> Task.FromResult<Result<E?, ValidationResult>>((E?)null);")]
    [InlineData("ValueTask<OneOf<E?, Failure>>", "=> new((E?)null);")]
    [InlineData("Result<OneOf<E?, Failure>, ValidationResult>", "=> ValidationResult.Error(\"rejected\");")]
    [InlineData("OneOf<E?, E?, ValidationResult>", "=> ValidationResult.Error(\"rejected\");")]
    public async Task should_warn_on_the_declared_event_branch(string signature, string body) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(signature, body), Warning());

    [Fact]
    public async Task should_recognize_an_event_type_alias() =>
        await VerifyCS.VerifyAnalyzerAsync("using EventAlias = E;\n" + Command("EventAlias?", "=> null;"), Warning());

    [Fact]
    public async Task should_recognize_inherited_event_metadata() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("Derived?", "=> null;") + "\npublic record Derived : E;", Warning(eventName: "Derived"));

    [Fact]
    public async Task should_report_an_inherited_handler_on_the_command() =>
        await VerifyCS.VerifyAnalyzerAsync(
            Preamble + @"

            public record Parent { public E? Handle() => null; }
            [Command] public record {|#0:C|} : Parent;
            ",
            Warning());
}
