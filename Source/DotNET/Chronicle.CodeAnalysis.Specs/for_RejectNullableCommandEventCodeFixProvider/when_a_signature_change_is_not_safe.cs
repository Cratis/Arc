// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.NullableCommandEventReturnAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.RejectNullableCommandEventCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RejectNullableCommandEventCodeFixProvider;

public class when_a_signature_change_is_not_safe
{
    [Theory]
    [InlineData("Task<E?>", "=> Task.FromResult<E?>(null);")]
    [InlineData("ValueTask<E?>", "=> new((E?)null);")]
    [InlineData("Result<E?, Failure>", "=> (E?)null;")]
    [InlineData("OneOf<E?, Failure, ValidationResult>", "=> ValidationResult.Error(\"rejected\");")]
    [InlineData("E?", "{ E? value = null; return value; }")]
    [InlineData("E?", "=> (E?)null;")]
    public async Task should_not_offer_a_non_compiling_or_contract_changing_fix(string signature, string body) =>
        await VerifyCS.VerifyNoCodeFixAsync(Command(signature, body), Warning());

    [Fact]
    public async Task should_not_change_an_inherited_handler() =>
        await VerifyCS.VerifyNoCodeFixAsync(
            Preamble + @"

            public record Parent { public E? Handle() => null; }
            [Command] public record {|#0:C|} : Parent;
            ",
            Warning());

    [Fact]
    public async Task should_not_change_a_virtual_handler() =>
        await VerifyCS.VerifyNoCodeFixAsync(Command("E?", "=> null;", "virtual "), Warning());

    [Fact]
    public async Task should_not_break_a_caller_in_the_same_project() =>
        await VerifyCS.VerifyNoCodeFixAsync(Command("E?", "=> null;") + "\npublic class Caller { public E? Call(C command) => command.Handle(); }", Warning());
}
