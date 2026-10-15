// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.with_an_atomic_batch_version_cap;

public class and_authoring_only_constructs : a_returned_event_batch
{
    [Fact]
    public void should_admit_atomic_batches_at_v22_without_an_authoring_override()
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint("=> builder.Unique<AuthorFiled>();"), authoring: true);
        AssertAtomicBatch();
        var capped = new ScreenplayEmitter().Emit(Result.Model, new ScreenplayOptions { AuthoringOnlyConstructs = true, MaximumExecutableModelVersion = SemanticVersion.V22 });

        capped.Source.ShouldEqual(Result.Source);
        capped.Diagnostics.Where(report => report.Code == "SP0070").ShouldBeEmpty();
    }

    [Fact]
    public void should_keep_unconstrained_batches_legacy_below_v22()
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", string.Empty, authoring: true);
        var capped = new ScreenplayEmitter().Emit(Result.Model, new ScreenplayOptions { AuthoringOnlyConstructs = true, MaximumExecutableModelVersion = SemanticVersion.V21 });

        capped.Source.ShouldNotContain("reaction batches");
        capped.Source.ShouldContain("produces AuthorWelcomed");
        capped.Source.ShouldContain("produces AuthorFiled");
        capped.Source.ShouldContain($"specification {ScenarioName}");
        capped.Diagnostics.Where(report => report.Code == "SP0070").ShouldBeEmpty();
    }
}
