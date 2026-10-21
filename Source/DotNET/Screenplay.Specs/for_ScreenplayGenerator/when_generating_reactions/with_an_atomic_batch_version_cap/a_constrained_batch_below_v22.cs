// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.with_an_atomic_batch_version_cap;

public class a_constrained_batch_below_v22 : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_the_legacy_file_reference_document_byte_for_byte(bool authoring)
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint("=> builder.Unique<AuthorFiled>();"), authoring);
        AssertAtomicBatch();
        var options = new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = SemanticVersion.V21 };
        var capped = new ScreenplayEmitter().Emit(Result.Model, options);
        var legacy = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Reactors = slice.Reactors.Select(reactor => reactor with { Reactions = [] }).ToList()
            }).ToList()
        };

        capped.Source.ShouldEqual(new ScreenplayEmitter().Emit(legacy, options).Source);
        capped.Source.ShouldNotContain("reaction batches");
        capped.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        capped.Source.ShouldNotContain($"specification {ScenarioName}");
        capped.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("ESM v22");
        capped.Diagnostics.Single(report => report.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("not stated declaratively");
    }
}
