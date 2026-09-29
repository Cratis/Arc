// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain;

/// <summary>
/// A resolver added through <see cref="ArcOptions.AddJsonTypeInfoResolver"/> goes ahead of Arc's resolver, and with it
/// ahead of anything appended to the chain, so it still wins.
/// </summary>
public class behind_a_resolver_added_through_arc_options : Specification
{
    ArcOptions _arcOptions;
    string _json;

    void Establish() => _arcOptions = new ArcOptions().AddJsonTypeInfoResolver(renaming_resolvers.RenamingQueryResultData("added"));

    void Because()
    {
        _arcOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(renaming_resolvers.RenamingQueryResultData("appended"));
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, _arcOptions.JsonSerializerOptions);
    }

    [Fact] void should_use_the_added_resolver() => _json.ShouldContain("\"added\":");
    [Fact] void should_not_use_the_appended_resolver() => _json.ShouldNotContain("\"appended\":");
}
