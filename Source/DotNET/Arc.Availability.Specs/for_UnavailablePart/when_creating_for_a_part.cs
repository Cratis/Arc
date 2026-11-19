// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;

namespace Cratis.Arc.Availability.for_UnavailablePart;

public class when_creating_for_a_part : Specification
{
    UnavailablePart _part;
    SliceLocation _location;

    void Because()
    {
        _location = SliceLocation.For(typeof(given.Fleet.Quality.ScoresReadModel));
        _part = UnavailablePart.For(typeof(given.Fleet.Quality.ScoresReadModel));
    }

    [Fact] void should_be_named_by_the_full_name_of_the_type() => _part.Name.ShouldEqual(typeof(given.Fleet.Quality.ScoresReadModel).FullName);
    [Fact] void should_carry_the_module_of_the_slice() => _part.Module.ShouldEqual(_location.Module.Value);
    [Fact] void should_carry_the_feature_of_the_slice() => _part.Feature.ShouldEqual(_location.Feature.Value);
    [Fact] void should_carry_the_slice() => _part.Slice.ShouldEqual(_location.Slice.Value);
}
