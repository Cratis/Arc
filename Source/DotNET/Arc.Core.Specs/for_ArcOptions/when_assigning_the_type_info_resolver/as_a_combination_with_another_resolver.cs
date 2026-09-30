// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_assigning_the_type_info_resolver;

/// <summary>
/// Assigning <c>Combine(options.TypeInfoResolver, other)</c> flattens into a chain of Arc's resolver followed by the other,
/// which makes Arc's resolver a top-level entry. The other resolver, like any resolver that follows Arc's, is consulted
/// first for the types it knows and wins.
/// </summary>
public class as_a_combination_with_another_resolver : Specification
{
    ArcOptions _arcOptions;
    IJsonTypeInfoResolver _other;
    string _json;

    void Establish()
    {
        _arcOptions = new ArcOptions();
        _other = renaming_resolvers.RenamingQueryResultData("payload");
    }

    void Because()
    {
        var options = _arcOptions.JsonSerializerOptions;
        options.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.TypeInfoResolver, _other);
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
    }

    [Fact] void should_flatten_arcs_resolver_ahead_of_the_other() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[0].ShouldBeOfExactType<ArcDefaultsJsonTypeInfoResolver>();
    [Fact] void should_follow_arcs_resolver_with_the_other() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[1].ShouldEqual(_other);
    [Fact] void should_let_the_other_resolver_win() => _json.ShouldContain("\"payload\":");
}
