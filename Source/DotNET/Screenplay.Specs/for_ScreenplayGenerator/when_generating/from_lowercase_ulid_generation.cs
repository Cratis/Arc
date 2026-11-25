// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_lowercase_ulid_generation : a_generation_strategy
{
    void Because() => Generate((Analyzed.SlicePath, Source("GenerateValue.Ulid().ToLowerInvariant()")));

    [Fact] void should_recover_the_declared_strategy() => AssertStrategy("generate ulid case lower");
}
