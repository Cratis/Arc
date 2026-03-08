// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.OpenApi.ModelBound;
using Microsoft.AspNetCore.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up model-bound OpenApi transformers.
/// </summary>
public static class ModelBoundOpenApiExtensions
{
    /// <summary>
    /// Adds model-bound OpenAPI operation transformers for Commands and Queries minimal APIs.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/>.</param>
    public static void AddModelBoundOperationTransformers(this OpenApiOptions options) =>
        options.AddOperationTransformer<CommandOperationTransformer>()
            .AddOperationTransformer<QueryOperationTransformer>();
}
