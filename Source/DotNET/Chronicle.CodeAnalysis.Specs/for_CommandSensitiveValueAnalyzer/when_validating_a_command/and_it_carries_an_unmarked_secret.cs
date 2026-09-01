// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

public class and_it_carries_an_unmarked_secret : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record PasswordChanged(Guid UserId);

    [Command]
    public record ChangePassword(Guid UserId, string {|#0:Password|})
    {
        public PasswordChanged Handle() => new(UserId);
    }
}",
                VerifyCS.Diagnostic("ARCCHR0009")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("ChangePassword", "Password")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
