// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace SampleApp;

[ReadModel]
public record WeatherReadModel(string City)
{
    public static WeatherReadModel GetByName(string city) => new(city);
}