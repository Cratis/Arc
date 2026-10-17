// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_non_uuid_comparison : a_generated_document
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Lending.Reserving;
        [EventType] public record BookReserved(string Title);
        [EventType] public record ReservationRefused(string Title);
        [Command] public record ReserveBook(string Title)
        {
            public object Handle() => Title == "Available" ? new BookReserved(Title) : new ReservationRefused(Title);
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_the_original_version_instead_of_selecting_v28(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = SemanticVersion.V27 }, (Analyzed.SlicePath, Source));
        Result.Source.ShouldContain("when title == \"Available\"");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("Uuid comparison", StringComparison.Ordinal) || diagnostic.Message.Contains("ESM v28", StringComparison.Ordinal)).ShouldBeFalse();
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }
}
