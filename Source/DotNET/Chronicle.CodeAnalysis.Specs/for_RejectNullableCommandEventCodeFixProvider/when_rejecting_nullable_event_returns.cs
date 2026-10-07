// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.NullableCommandEventReturnAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.RejectNullableCommandEventCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RejectNullableCommandEventCodeFixProvider;

public class when_rejecting_nullable_event_returns
{
    const string Result = "global::Cratis.Monads.Result<global::E, global::Cratis.Arc.Validation.ValidationResult>";
    const string Error = "global::Cratis.Arc.Validation.ValidationResult.Error(\"TODO: explain why\")";

    [Theory]
    [InlineData("=> null;")]
    [InlineData("=> true ? new E() : null;")]
    [InlineData("=> false ? null : new E();")]
    [InlineData("=> true ? (false ? null : new E()) : null;")]
    [InlineData("{ if (true) return null; return new E(); }")]
    [InlineData("{ string? unused = null; return null; }")]
    [InlineData("{ E? Local() => null; Func<E?> factory = () => null; return null; }")]
    public async Task should_replace_only_null_return_branches(string body)
    {
        var fixedBody = body.Replace("return null", "return " + Error, StringComparison.Ordinal);
        if (body.StartsWith("=>", StringComparison.Ordinal))
        {
            fixedBody = body.Replace("null", Error, StringComparison.Ordinal);
        }

        await VerifyCS.VerifyCodeFixAsync(Command("E?", body), SourceMarker.Parse(Command(Result, fixedBody)).Source, Warning());
    }

    [Theory]
    [InlineData("Task<E?>", "Task<")]
    [InlineData("ValueTask<E?>", "ValueTask<")]
    [InlineData("System.Threading.Tasks.Task<E?>", "System.Threading.Tasks.Task<")]
    public async Task should_keep_the_async_wrapper(string signature, string wrapper)
    {
        const string body = "{ await Task.Yield(); return true ? new E() : null; }";
        var fixedBody = body.Replace("null", Error, StringComparison.Ordinal);
        await VerifyCS.VerifyCodeFixAsync(Command(signature, body, "async "), SourceMarker.Parse(Command(wrapper + Result + ">", fixedBody, "async ")).Source, Warning());
    }

    [Theory]
    [InlineData("Result<E?, ValidationResult>")]
    [InlineData("OneOf<E?, ValidationResult>")]
    [InlineData("OneOf<ValidationResult, E?>")]
    public async Task should_preserve_the_event_or_validation_contract(string signature) =>
        await VerifyCS.VerifyCodeFixAsync(Command(signature, "=> true ? new E() : null;"), SourceMarker.Parse(Command(Result, "=> true ? new E() : " + Error + ";")).Source, Warning());

    [Fact]
    public async Task should_leave_an_unrelated_command_warning_in_place()
    {
        const string other = "\n[Command] public record Other { public {|#1:E?|} Handle() => null; }";
        await VerifyCS.VerifyCodeFixAsync(
            Command("E?", "=> null;") + other,
            SourceMarker.Parse(Command(Result, "=> " + Error + ";") + other).Source,
            Warning(),
            Warning("Other"));
    }
}
