// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_CoordinateSerializer.when_deserializing;

public class a_document : given.a_coordinate_serializer
{
    Coordinate _result;

    void Because() => _result = Deserialize(new BsonDocument
    {
        { "longitude", 10.5 },
        { "latitude", 20.25 }
    });

    [Fact] void should_read_the_longitude() => _result.Longitude.ShouldEqual(10.5);
    [Fact] void should_read_the_latitude() => _result.Latitude.ShouldEqual(20.25);
}
