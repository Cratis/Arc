// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;

namespace Cratis.Arc.MongoDB.for_CoordinateSerializer.when_roundtripping;

public class a_coordinate : given.a_coordinate_serializer
{
    Coordinate _original;
    Coordinate _result;

    void Establish() => _original = new Coordinate(-122.4194, 37.7749);

    void Because() => _result = Deserialize(Serialize(_original));

    [Fact] void should_preserve_the_coordinate() => _result.ShouldEqual(_original);
}
