// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="IArcBuilder"/> for adding Chronicle support.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Adds Chronicle support to Arc.
    /// </summary>
    /// <param name="builder">The <see cref="IArcBuilder"/> to add to.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithChronicle(this IArcBuilder builder)
    {
        builder.Services.AddAggregateRoots(builder.Types);
        builder.Services.AddReadModels(builder.Types);
        return builder;
    }
}
