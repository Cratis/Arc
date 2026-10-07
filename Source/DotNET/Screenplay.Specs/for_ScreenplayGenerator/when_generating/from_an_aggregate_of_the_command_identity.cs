// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_aggregate_of_the_command_identity : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, AggregateRoutingSources.With("""
        public async Task Handle(IAggregateRootFactory factory)
        {
            var author = await factory.Get<Author>(Id);
            await author.Register(Name);
            await author.Commit();
        }
        """)));

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_state_the_command_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_report_no_destination_loss() => Result.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeFalse();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
