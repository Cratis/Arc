// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver;

/// <summary>
/// When the application has cleared the options' resolver, the added resolver must not be left alone in the chain: Arc's
/// resolver is composed anew behind it, so the wire types the added resolver does not know keep serializing as they
/// do by default, through Arc's metadata and the reflection-based fallback.
/// </summary>
public class after_clearing_the_type_info_resolver : Specification
{
    ArcOptions _arcOptions;
    IJsonTypeInfoResolver _resolver;
    string _queryResultJson;
    string _commandResultJson;
    string _commandResultJsonWithArcDefaults;

    void Establish()
    {
        _arcOptions = new ArcOptions();
        _arcOptions.JsonSerializerOptions.TypeInfoResolver = null;
        _resolver = new a_resolver_knowing_only_the_query_result("payload");
    }

    void Because()
    {
        _arcOptions.AddJsonTypeInfoResolver(_resolver);
        var options = _arcOptions.JsonSerializerOptions;
        _queryResultJson = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
        _commandResultJson = JsonSerializer.Serialize(representative_wire_values.CommandResult, options);
        _commandResultJsonWithArcDefaults = JsonSerializer.Serialize(representative_wire_values.CommandResult, new ArcOptions().JsonSerializerOptions);
    }

    [Fact] void should_put_the_resolver_first() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[0].ShouldEqual(_resolver);
    [Fact] void should_compose_arcs_resolver_behind_it() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[1].ShouldBeOfExactType<ArcDefaultsJsonTypeInfoResolver>();
    [Fact] void should_apply_the_added_resolvers_contract() => _queryResultJson.ShouldContain("\"payload\":");
    [Fact] void should_serialize_the_types_it_does_not_know_as_by_default() => _commandResultJson.ShouldEqual(_commandResultJsonWithArcDefaults);
}
