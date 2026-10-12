// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class an_enumeration_with_positional_pins : a_positional_enumeration_document
{
    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V11 }, (Analyzed.SlicePath, Source));

    [Fact] void should_pin_below_v12_without_changing_executable_version_or_revision() => AssertPositionalDocument();
    [Fact] void should_report_nothing() => Result.Diagnostics.ShouldBeEmpty();
}
