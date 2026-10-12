// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_EmittedIdentityCompliance.given;

public class a_document : Specification
{
    protected const string Source = """
        concept ProtectedValue : String

        module Library
          feature Accounts
            slice StateChange Changing
              event Saved
                value ProtectedValue

              command Change
                id String identifier
                destination ProtectedValue
                produces Saved
                  for id
                  value = destination
        """;

    protected ScreenplayDiagnostics Diagnostics;
    protected ApplicationSyntax Application;
    protected ApplicationSyntax Result;

    protected void Establish()
    {
        var compiled = new ScreenplayCompiler().Compile(Source);
        compiled.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        Application = compiled.Value!;
        Diagnostics = new();
        Application = Application with
        {
            Concepts = Application.Concepts.Select(concept => concept with
            {
                Attributes = [new ConceptAttributeSyntax(ConceptAttributeSyntax.Pii, SourceLocation.Start), new ConceptAttributeSyntax(ConceptAttributeSyntax.Sensitive, SourceLocation.Start)]
            }).ToList()
        };
    }

    protected void Apply() => Result = new EmittedIdentityCompliance(Application).Apply(Diagnostics);

    protected void AssertStripped(string use, bool compiles = true)
    {
        Result.Concepts.Single().Attributes.ShouldBeEmpty();
        Diagnostics.All.Single().Message.ShouldContain(use);
        if (!compiles)
        {
            return;
        }

        var compiled = new ScreenplayCompiler().Compile(new ScreenplayPrinter().Print(Result));
        compiled.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        var roundTrip = Verification.RoundTrip.For(Result);
        roundTrip.Errors.ShouldBeEmpty();
        roundTrip.IsStable.ShouldBeTrue();
    }

    protected void ChangeCommand(Func<CommandSyntax, CommandSyntax> change)
    {
        var module = Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        Application = Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = slice.Commands.Select(change).ToList() }] }] }]
        };
    }
}
