// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

public class and_the_command_itself_is_not_audited : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record CredentialsReset(Guid UserId);

    [Command]
    [NotAudited]
    public record ResetCredentials(Guid UserId, string Password, string RecoveryToken)
    {
        public CredentialsReset Handle() => new(UserId);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
