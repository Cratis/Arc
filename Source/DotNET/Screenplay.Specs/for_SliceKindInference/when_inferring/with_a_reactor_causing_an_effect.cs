// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_inferring;

/// <summary>
/// A reactor decides the kind of the slice even when the slice also holds a command - what the slice is about is
/// the reaction, not the intent that happens to live alongside it.
/// </summary>
public class with_a_reactor_causing_an_effect : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Infer(
        LibraryAuthors.Registration().Commands,
        LibraryLending.Notifications().Reactors);

    [Fact] void should_infer_automation_even_though_there_is_a_command() => _result.ShouldEqual(SliceKind.Automation);
}
