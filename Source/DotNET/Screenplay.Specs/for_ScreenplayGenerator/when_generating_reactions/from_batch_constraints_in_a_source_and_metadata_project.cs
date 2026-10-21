// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Arc.Screenplay.Verification;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_batch_constraints_in_a_source_and_metadata_project : a_returned_event_batch
{
    [Theory]
    [InlineData("{ builder.Unique<AuthorFiled>(); }", false)]
    [InlineData("{ builder.Unique<AuthorFiled>(); }", true)]
    [InlineData("{ builder.Unique<AuthorRegistered>(); builder.ClosesStreamOn<AuthorWelcomed>(); }", false)]
    [InlineData("{ builder.Unique<AuthorRegistered>(); builder.ClosesStreamOn<AuthorWelcomed>(); }", true)]
    [InlineData("{ builder.Unique<AuthorFiled>(); builder.ClosesStreamOn<AuthorWelcomed>(); }", true)]
    public void should_match_coverage_across_source_and_binary_event_symbols(string definition, bool authoring)
    {
        var files = new (string Path, string Text)[]
        {
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Contracts.cs", Welcoming),
            ("Library/Authors/Welcoming/Welcomer.cs", """
                using System.Collections.Generic;
                using Cratis.Chronicle.Reactors;
                using Library.Authors.Registration;
                namespace Library.Authors.Welcoming;
                public class Welcomer : IReactor
                {
                    public IEnumerable<object> Welcome(AuthorRegistered @event) => [new AuthorWelcomed(@event.Name, "Welcome"), new AuthorFiled(@event.Country)];
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            (ScenarioPath, Scenario)
        };
        var contracts = Analyzed.Project("Library.Contracts", [], files);
        using var stream = new MemoryStream();
        var emitted = contracts.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var application = Analyzed.Project("Rules", [MetadataReference.CreateFromImage(stream.ToArray())], ("Library/Authors/Welcoming/Rules.cs", "using Library.Authors.Registration; namespace Library.Authors.Welcoming;" + Constraint(definition)));
        Analyzed.ErrorsIn(application).ShouldBeEmpty();

        Result = new ScreenplayGenerator().Generate([contracts, application], new ScreenplayOptions { Domain = "Library", Module = "Library", AuthoringOnlyConstructs = authoring });

        if (definition.Contains("ClosesStreamOn", StringComparison.Ordinal))
        {
            Assert.Matches(@"when AuthorRegistered\s+file \S*Welcomer\.cs", Result.Source);
            Result.Source.ShouldNotContain($"specification {ScenarioName}");
            Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("Cratis/Screenplay#770");
            ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.UnreadableSpecification);
        }
        else
        {
            Result.Source.ShouldContain("reaction batches atomic");
            Result.Source.ShouldContain("produces AuthorFiled");
            Result.Source.ShouldContain($"specification {ScenarioName}");
            Result.Diagnostics.Where(_ => _.Code == "SP0070").ShouldBeEmpty();
        }
        var verified = new ScreenplayVerifier().Verify(Result.Source);
        verified.Compiles.ShouldBeTrue();
        verified.BindingDiagnostics.Where(_ => _.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        Verification.RoundTrip.For(verified.Application!).IsStable.ShouldBeTrue();
    }
}
