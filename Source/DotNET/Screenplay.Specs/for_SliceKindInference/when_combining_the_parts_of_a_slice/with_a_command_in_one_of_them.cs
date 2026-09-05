// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_combining_the_parts_of_a_slice;

/// <summary>
/// A contracts project holding only the events of a slice reads as a state view on its own, because nothing in it
/// changes anything. The command in the project beside it is what the slice is really about, and joining the two
/// has to say so.
/// </summary>
public class with_a_command_in_one_of_them : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Combine([SliceKind.StateView, SliceKind.StateChange]);

    [Fact] void should_infer_state_change() => _result.ShouldEqual(SliceKind.StateChange);
}
