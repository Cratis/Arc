// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace MetaConsumerApp;

[ReadModel]
public record SampleMetaReadModel(string City)
{
    public static SampleMetaReadModel GetByName(string city) => new(city);
}
