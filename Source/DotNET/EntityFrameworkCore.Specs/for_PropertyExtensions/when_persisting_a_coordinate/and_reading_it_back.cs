// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_PropertyExtensions.when_persisting_a_coordinate;

public class and_reading_it_back : given.a_database_with_a_coordinate_property
{
    Place _stored;

    async Task Because()
    {
        _context.Places.Add(new Place { Id = 1, Location = new Coordinate(-122.4194, 37.7749) });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _stored = await _context.Places.SingleAsync(_ => _.Id == 1);
    }

    [Fact] void should_preserve_the_longitude() => _stored.Location.Longitude.ShouldEqual(-122.4194);
    [Fact] void should_preserve_the_latitude() => _stored.Location.Latitude.ShouldEqual(37.7749);
}
