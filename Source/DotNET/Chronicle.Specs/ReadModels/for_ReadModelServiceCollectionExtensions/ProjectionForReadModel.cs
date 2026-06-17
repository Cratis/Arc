// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

/// <summary>
/// A projection for the <see cref="ProjectionReadModel"/>.
/// </summary>
public class ProjectionForReadModel : IProjectionFor<ProjectionReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ProjectionReadModel> builder)
    {
    }
}
