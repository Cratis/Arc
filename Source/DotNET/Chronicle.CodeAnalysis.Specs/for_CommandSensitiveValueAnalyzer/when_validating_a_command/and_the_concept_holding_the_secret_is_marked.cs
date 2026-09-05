// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

/// <summary>
/// Marking the concept once is how an application covers every command that takes one, and the runtime honors a
/// marking on the property's type. Reporting it here would be reporting code that is already correct - and would
/// push people to repeat the marking on every use of the concept for no gain.
/// </summary>
public class and_the_concept_holding_the_secret_is_marked : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Concepts;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [NotAudited]
    public record ProviderApiKey(string Value) : ConceptAs<string>(Value);

    public record ProviderAdded(Guid ProviderId);

    [Command]
    public record AddProvider(Guid ProviderId, ProviderApiKey ApiKey)
    {
        public ProviderAdded Handle() => new(ProviderId);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
