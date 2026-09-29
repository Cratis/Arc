// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver;

/// <summary>
/// Once the application has wrapped the options' resolver, Arc's resolver is no longer a top-level entry of the chain. A
/// resolver added afterwards still goes ahead of the wrapper, so it is consulted first and its contracts win.
/// </summary>
public class after_wrapping_the_type_info_resolver : Specification
{
    ArcOptions _arcOptions;
    IJsonTypeInfoResolver _resolver;
    string _json;

    void Establish()
    {
        _arcOptions = new ArcOptions();
        _resolver = renaming_resolvers.RenamingQueryResultData("payload");
        var options = _arcOptions.JsonSerializerOptions;
        options.TypeInfoResolver = options.TypeInfoResolver!.WithAddedModifier(renaming_resolvers.RenameQueryResultData("wrapped"));
    }

    void Because()
    {
        _arcOptions.AddJsonTypeInfoResolver(_resolver);
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, _arcOptions.JsonSerializerOptions);
    }

    [Fact] void should_put_the_resolver_ahead_of_the_wrapper() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[0].ShouldEqual(_resolver);
    [Fact] void should_keep_the_wrapper() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain.Count.ShouldEqual(2);
    [Fact] void should_apply_the_added_resolvers_contract() => _json.ShouldContain("\"payload\":");
    [Fact] void should_not_apply_the_wrappers_modifier() => _json.ShouldNotContain("\"wrapped\":");
}
