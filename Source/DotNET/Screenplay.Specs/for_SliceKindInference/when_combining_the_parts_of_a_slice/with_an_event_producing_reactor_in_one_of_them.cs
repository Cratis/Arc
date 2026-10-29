// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_combining_the_parts_of_a_slice;

/// <summary>
/// An event-producing reaction remains an automation when combined with a state-changing part.
/// </summary>
public class with_an_event_producing_reactor_in_one_of_them : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Combine([SliceKind.Automation, SliceKindInference.Infer([], LibraryLending.Restocking().Reactors), SliceKind.StateChange]);

    [Fact] void should_infer_automation() => _result.ShouldEqual(SliceKind.Automation);
}
