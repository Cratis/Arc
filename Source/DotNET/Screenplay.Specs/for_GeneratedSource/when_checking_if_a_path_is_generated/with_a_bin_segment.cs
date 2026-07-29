// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_GeneratedSource.when_checking_if_a_path_is_generated;

public class with_a_bin_segment : Specification
{
    bool _result;

    void Because() => _result = GeneratedSource.Is("/src/Core/bin/Release/net10.0/Slice.cs");

    [Fact] void should_recognize_it_as_generated() => _result.ShouldBeTrue();
}
