// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_adding_chronicle;

public class without_any_provider : given.an_arc_builder
{
    IArcBuilder _result;

    void Because() => _result = _builder.WithChronicle();

    [Fact] void should_return_the_builder() => _result.ShouldEqual(_builder);
}
