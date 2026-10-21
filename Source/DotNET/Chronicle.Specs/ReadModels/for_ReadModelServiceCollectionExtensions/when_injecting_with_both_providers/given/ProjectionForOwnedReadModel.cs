// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_injecting_with_both_providers.given;

public class ProjectionForOwnedReadModel : IProjectionFor<ProjectionOwnedReadModel>
{
    public void Define(IProjectionBuilderFor<ProjectionOwnedReadModel> builder)
    {
    }
}
