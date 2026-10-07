// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_aggregate_fetched_for_another_identity : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, AggregateRoutingSources.With("""
        public async Task Handle(IAggregateRootFactory factory)
        {
            var author = await factory.Get<Author>(OtherId);
            await author.Register(Name);
            await author.Commit();
        }
        """)));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_not_retarget_the_aggregate() => Result.Source.ShouldNotContain("for id");
    [Fact] void should_not_mark_the_context_identifier() => Result.Source.ShouldNotContain(" identifier");
    [Fact] void should_report_the_unrepresented_destination() => Result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
