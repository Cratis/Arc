// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_inheriting_a_key_provider : Specification
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; using Cratis.Chronicle.Projections.ModelBound; using Cratis.Chronicle.Events; " +
        "public class BaseCommand { public virtual void Handle([LookUpBy<CallerSubject>] Model model) { } } " +
        "public class Command : BaseCommand { public override void Handle([LookUpBy(\"Key\")] Model model) { } } " +
        "[FromEvent<Changed>] public class Model; " +
        "[EventType(\"5908fb8b-80a7-45e8-8157-b6af86425c19\")] public record Changed;",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "only one lookup form"));

    [Fact] Task should_reject_the_inherited_conflict() => _result;
}
