// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_inferring;

public class without_commands_or_reactors : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Infer([], []);

    [Fact] void should_infer_state_view() => _result.ShouldEqual(SliceKind.StateView);
}
