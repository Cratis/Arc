// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class an_enumeration_with_positional_pins : a_positional_enumeration_document
{
    void Because() => Generate((Analyzed.SlicePath, Source));

    [Fact] void should_pin_without_changing_executable_version_or_revision() => AssertPositionalDocument();
    [Fact] void should_report_nothing() => Result.Diagnostics.ShouldBeEmpty();
}
