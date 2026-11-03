// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider.given;

public class a_generated_provider_with_an_assembly_filter : a_provider_with_an_assembly_filter
{
    void Establish()
    {
        _queryName = $"Generated.Metadata.ReadModel.{nameof(PublicReadModelWithValidQuery.GetById)}";
        _registry.All.Returns(new Dictionary<string, Type> { [_queryName] = typeof(PublicReadModelWithValidQuery) });
    }
}
