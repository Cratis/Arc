// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Slices;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_reading_then_before_run : given.a_slice
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = _slice.Then);

    [Fact] void should_require_a_run() => _error.ShouldBeOfExactType<SliceNotRun>();
}
