// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_ulid_generation_strategy : a_generation_strategy
{
    void Because() => Generate((Analyzed.SlicePath, Source("GenerateValue.Ulid()")));

    [Fact] void should_recover_uppercase_crockford_generation() => AssertStrategy("generate ulid");
}
