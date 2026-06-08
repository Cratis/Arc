// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_coordinate_is_known_type;

public class a_coordinate : Specification
{
    bool _result;

    void Because() => _result = typeof(Coordinate).IsKnownType();

    [Fact] void should_be_a_known_type() => _result.ShouldBeTrue();
}
