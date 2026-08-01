// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_combining_the_parts_of_a_slice;

public class without_a_part_that_changes_anything : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Combine([SliceKind.StateView, SliceKind.StateView]);

    [Fact] void should_infer_state_view() => _result.ShouldEqual(SliceKind.StateView);
}
