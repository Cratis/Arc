// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

/// <summary>
/// Only a command's values reach the causation chain, so a plain type carrying a secret is nothing to do with this
/// rule.
/// </summary>
public class and_the_type_is_not_a_command : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;

namespace TestNamespace
{
    public record Credentials(Guid UserId, string Password, string ApiKey);
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
