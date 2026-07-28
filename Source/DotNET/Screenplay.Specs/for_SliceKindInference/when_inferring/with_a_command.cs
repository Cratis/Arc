// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_inferring;

public class with_a_command : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Infer(LibraryAuthors.Registration().Commands, []);

    [Fact] void should_infer_state_change() => _result.ShouldEqual(SliceKind.StateChange);
}
