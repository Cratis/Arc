// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_combining_the_parts_of_a_slice;

/// <summary>
/// A reactor turning an event into further events adapts one part of the model into another, which outranks every
/// other reading of the slice - including the automation another part of it would be on its own.
/// </summary>
public class with_a_translating_reactor_in_one_of_them : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Combine([SliceKind.Automation, SliceKind.Translate, SliceKind.StateChange]);

    [Fact] void should_infer_translate() => _result.ShouldEqual(SliceKind.Translate);
}
