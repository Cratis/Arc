// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

/// <summary>
/// Chronicle already withholds personal data from the causation, so a value marked that way is not going to be
/// written and there is nothing left to warn about.
/// </summary>
public class and_the_secret_is_marked_as_personal_data : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Compliance.GDPR;

namespace TestNamespace
{
    public record PinChanged(Guid UserId);

    [Command]
    public record ChangePin(Guid UserId, [property: PII(""The customer's chosen pin"")] string Pin)
    {
        public PinChanged Handle() => new(UserId);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
