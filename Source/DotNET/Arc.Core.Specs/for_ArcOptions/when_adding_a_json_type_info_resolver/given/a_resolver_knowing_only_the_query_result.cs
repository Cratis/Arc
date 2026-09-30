// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver.given;

/// <summary>
/// A resolver that customizes <see cref="QueryResult"/> and knows no other type, the way a source-generated context that
/// covers only some of Arc's wire types does.
/// </summary>
/// <param name="name">The name to write <see cref="QueryResult.Data"/> under.</param>
public class a_resolver_knowing_only_the_query_result(string name) : IJsonTypeInfoResolver
{
    readonly DefaultJsonTypeInfoResolver _inner = renaming_resolvers.RenamingQueryResultData(name);

    /// <inheritdoc/>
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) =>
        type == typeof(QueryResult) ? _inner.GetTypeInfo(type, options) : null;
}
