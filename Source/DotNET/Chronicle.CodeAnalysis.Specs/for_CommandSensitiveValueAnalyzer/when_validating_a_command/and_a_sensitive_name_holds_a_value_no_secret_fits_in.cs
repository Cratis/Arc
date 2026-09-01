// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

/// <summary>
/// A name is weak evidence on its own. "AccessTokenExpiresAt" contains the word "token" and holds a date - it is a
/// timestamp, and reporting it would teach people that the rule guesses badly, which is how a rule ends up
/// suppressed wholesale.
/// </summary>
public class and_a_sensitive_name_holds_a_value_no_secret_fits_in : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record AccountConnected(Guid AccountId);

    [Command]
    public record ConnectAccount(
        Guid AccountId,
        DateTimeOffset AccessTokenExpiresAt,
        DateTime RefreshTokenIssuedAt,
        TimeSpan TokenLifetime,
        int PinLength,
        bool SecretRotationEnabled)
    {
        public AccountConnected Handle() => new(AccountId);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
