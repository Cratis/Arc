// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_GeneratedSource.when_checking_if_a_path_is_generated;

public class with_an_authored_path : Specification
{
    bool _result;

    void Because() => _result = GeneratedSource.Is("/src/Core/Feature/Slice/Slice.cs");

    [Fact] void should_not_recognize_it_as_generated() => _result.ShouldBeFalse();
}
