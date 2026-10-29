// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_combining_the_parts_of_a_slice;

public class with_an_explicit_translation_in_one_of_them : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Combine([SliceKind.Automation, SliceKind.Translate, SliceKind.StateChange]);

    [Fact] void should_preserve_the_explicit_translation() => _result.ShouldEqual(SliceKind.Translate);
}
