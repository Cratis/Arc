// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_SliceContent;

/// <summary>
/// A read model whose projection could not be expressed, with no query onto it, leaves a slice with nothing to
/// declare. The header alone is not a valid slice body, so the generator has to drop it.
/// </summary>
public class when_checking_a_slice_with_nothing_declared : Specification
{
    SliceSyntax _empty;
    SliceSyntax _withOnlyAnEvent;
    SliceSyntax _withOnlyADescription;

    void Establish()
    {
        _empty = Slice(null);
        _withOnlyADescription = Slice("Reserves a copy of a book.");
        _withOnlyAnEvent = _empty with
        {
            Events = [new EventSyntax("BookReserved", [], SourceLocation.Start)]
        };
    }

    [Fact] void should_report_a_slice_with_nothing_as_empty() => SliceContent.IsEmpty(_empty).ShouldBeTrue();
    [Fact] void should_not_report_a_slice_with_an_event_as_empty() => SliceContent.IsEmpty(_withOnlyAnEvent).ShouldBeFalse();
    [Fact] void should_not_report_a_slice_carrying_only_a_description_as_empty() => SliceContent.IsEmpty(_withOnlyADescription).ShouldBeFalse();

    static SliceSyntax Slice(string? description) =>
        new(SliceType.StateView, "Listing", [], [], [], [], [], [], [], [], [], SourceLocation.Start, description);
}
