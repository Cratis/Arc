// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_combining_the_parts_of_a_slice;

/// <summary>
/// A reactor decides the kind of a slice even when a command sits alongside it, and it decides it just as much when
/// the command was written in another project - what the slice is about is the reaction wherever the two halves of
/// it happen to live.
/// </summary>
public class with_a_reactor_in_one_of_them : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Combine([SliceKind.StateChange, SliceKind.Automation]);

    [Fact] void should_infer_automation() => _result.ShouldEqual(SliceKind.Automation);
}
