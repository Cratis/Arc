// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_SliceKindInference.when_inferring;

/// <summary>
/// A reaction producing further events is an automation, not evidence of external vocabulary translation.
/// </summary>
public class with_a_reactor_producing_further_events : Specification
{
    SliceKind _result;

    void Because() => _result = SliceKindInference.Infer([], LibraryLending.Restocking().Reactors);

    [Fact] void should_infer_automation() => _result.ShouldEqual(SliceKind.Automation);
}
