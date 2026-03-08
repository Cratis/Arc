// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.OpenApi;
using Microsoft.AspNetCore.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up OpenAPI for a Cratis application.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds Cratis OpenAPI schema and operation transformers.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/>.</param>
    public static void AddConcepts(this OpenApiOptions options) =>
        options.AddSchemaTransformer<ConceptSchemaTransformer>()
            .AddSchemaTransformer<EnumSchemaTransformer>()
            .AddSchemaTransformer<FromRequestSchemaTransformer>()
            .AddOperationTransformer<FromRequestOperationTransformer>()
            .AddOperationTransformer<CommandResultOperationTransformer>()
            .AddOperationTransformer<QueryResultOperationTransformer>()
            .AddModelBoundOperationTransformers();
}
