// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Source fragments form independent compilation units.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_using_key_providers
{
    const string Imports = "using Cratis.Arc.Commands; ";

    [Fact]
    public Task should_accept_a_read_model_provider() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(Imports +
        "public class Command { public void Handle([LookUpBy<CallerSubject>] Model model) { } } [Cratis.Chronicle.Projections.ModelBound.FromEvent<Changed>] public class Model; [Cratis.Chronicle.Events.EventType(\"5908fb8b-80a7-45e8-8157-b6af86425c19\")] public record Changed;");

    [Fact]
    public Task should_reject_a_provider_on_a_non_read_model() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(Imports +
        "public class Command { public void Handle([LookUpBy<CallerSubject>] string model) { } }",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "not a read-model type"));

    [Theory]
    [InlineData("LookUpBy(\"Key\"), LookUpBy<CallerSubject>")]
    public Task should_reject_conflicting_lookup_forms(string attributes) => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(Imports +
        $"public class Command {{ public string Key {{ get; }} public void Handle([{attributes}] Model model) {{ }} }} public class Model; public sealed class LoginClaim() : CallerClaim(\"login\");",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "only one lookup form"));

    [Fact]
    public async Task should_reject_two_providers_even_before_analyzer_execution()
    {
        var project = TestProject.CreateProject(Imports + "public class Command { public void Handle([LookUpBy<CallerSubject>, LookUpBy<LoginClaim>] Model model) { } } public class Model; public sealed class LoginClaim() : CallerClaim(\"login\");");
        var compilation = await project.GetCompilationAsync(TestContext.Current.CancellationToken);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(diagnostic => diagnostic.Id == "CS0579");
        var command = compilation.GetTypeByMetadataName("Command");
        var parameter = command.GetMembers("Handle").OfType<IMethodSymbol>().Single().Parameters[0];
        Cratis.Arc.Commands.CommandAttributeSymbols.ResolveKeyProvider(parameter).Conflict.ShouldEqual("InvalidLookUpBy");
    }
}
