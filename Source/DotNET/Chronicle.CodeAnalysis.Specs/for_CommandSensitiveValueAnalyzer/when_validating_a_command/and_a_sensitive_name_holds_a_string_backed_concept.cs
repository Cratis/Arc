// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

/// <summary>
/// Wrapping a secret in a concept does not stop it being recorded - the concept is unwrapped to the value it
/// carries - so it must not stop it being reported either.
/// </summary>
public class and_a_sensitive_name_holds_a_string_backed_concept : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Concepts;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record AccessToken(string Value) : ConceptAs<string>(Value);

    public record AccountConnected(Guid AccountId);

    [Command]
    public record ConnectAccount(Guid AccountId, AccessToken {|#0:Token|})
    {
        public AccountConnected Handle() => new(AccountId);
    }
}",
                VerifyCS.Diagnostic("ARCCHR0009")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("ConnectAccount", "Token")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
