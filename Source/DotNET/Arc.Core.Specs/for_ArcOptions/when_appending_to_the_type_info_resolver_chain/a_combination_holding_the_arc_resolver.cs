// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain;

/// <summary>
/// An appended resolver that combines the options' own resolver with another holds Arc's resolver, which consults the
/// appended resolvers first. Arc's resolver must not consult itself again for the same type, and the combination
/// resolves in the order it states: the options' own resolver - Arc's metadata and reflection - ahead of the other.
/// </summary>
public class a_combination_holding_the_arc_resolver : Specification
{
    ArcOptions _arcOptions;
    string _json;
    string _jsonWithArcDefaults;

    void Establish() => _arcOptions = new ArcOptions();

    void Because()
    {
        var options = _arcOptions.JsonSerializerOptions;
        options.TypeInfoResolverChain.Add(JsonTypeInfoResolver.Combine(options.TypeInfoResolver, renaming_resolvers.RenamingQueryResultData("payload")));
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
        _jsonWithArcDefaults = JsonSerializer.Serialize(representative_wire_values.QueryResult, new ArcOptions().JsonSerializerOptions);
    }

    [Fact] void should_resolve_through_the_options_own_resolver_first() => _json.ShouldEqual(_jsonWithArcDefaults);
}
