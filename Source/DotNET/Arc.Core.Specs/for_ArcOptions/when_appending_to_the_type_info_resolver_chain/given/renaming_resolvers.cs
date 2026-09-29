// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.Queries;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;

/// <summary>
/// Custom-contract resolvers of the kind an application appends to customize how members are written.
/// </summary>
public static class renaming_resolvers
{
    /// <summary>
    /// Create a reflection-based resolver that writes <see cref="QueryResult.Data"/> under another name.
    /// </summary>
    /// <param name="name">The name to write the data under.</param>
    /// <returns>The <see cref="IJsonTypeInfoResolver"/>.</returns>
    public static DefaultJsonTypeInfoResolver RenamingQueryResultData(string name) => new()
    {
        Modifiers =
        {
            typeInfo =>
            {
                if (typeInfo.Type != typeof(QueryResult))
                {
                    return;
                }

                foreach (var property in typeInfo.Properties.Where(_ => _.Name == "data"))
                {
                    property.Name = name;
                }
            }
        }
    };
}
