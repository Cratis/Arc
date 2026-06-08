// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;

namespace Cratis.Arc.EntityFrameworkCore.for_PropertyExtensions;

public class Place
{
    public int Id { get; set; }
    public Coordinate Location { get; set; } = new(0, 0);
}
