// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_CoordinateSerializer.when_serializing;

public class a_coordinate : given.a_coordinate_serializer
{
    BsonDocument _result;

    void Because() => _result = Serialize(new Coordinate(10.5, 20.25));

    [Fact] void should_write_the_longitude() => _result["longitude"].AsDouble.ShouldEqual(10.5);
    [Fact] void should_write_the_latitude() => _result["latitude"].AsDouble.ShouldEqual(20.25);
    [Fact] void should_write_only_the_two_expected_elements() => _result.ElementCount.ShouldEqual(2);
}
