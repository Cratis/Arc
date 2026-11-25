// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.NullableCommandEventReturnAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.RejectNullableCommandEventCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_RejectNullableCommandEventCodeFixProvider;

public class when_returning_native_union_events : Specification
{
    [Theory]
    [InlineData("Outcome", "default")]
    [InlineData("Optional", "new E()")]
    public Task should_withhold_a_contract_changing_fix(string signature, string expression) => VerifyCS.VerifyNoCodeFixAsync(
        Command(signature, "=> " + expression + ";") + "public union Outcome(E, Failure); public union Optional(E?, Failure);", Warning());
}
#endif
