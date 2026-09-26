// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Defers converter-handled child schemas until OpenAPI has registered their components.
/// </summary>
internal static class DeferredSchemas
{
    static readonly ConditionalWeakTable<OpenApiDocument, List<(Type Type, OpenApiSchema Schema, JsonSerializerOptions Options)>> _pending = new();

    /// <summary>
    /// Records a schema for expansion after component references have been resolved.
    /// </summary>
    /// <param name="schema">The schema to expand.</param>
    /// <param name="context">The schema context.</param>
    public static void Register(OpenApiSchema schema, OpenApiSchemaTransformerContext context) =>
        _pending.GetOrCreateValue(context.Document).Add((context.JsonTypeInfo.Type, schema, context.JsonTypeInfo.Options));

    /// <summary>
    /// Gets the recorded schemas for one document.
    /// </summary>
    /// <param name="document">The document being generated.</param>
    /// <returns>The schemas awaiting expansion.</returns>
    public static IReadOnlyList<(Type Type, OpenApiSchema Schema, JsonSerializerOptions Options)> For(OpenApiDocument document) =>
        _pending.GetOrCreateValue(document);
}
