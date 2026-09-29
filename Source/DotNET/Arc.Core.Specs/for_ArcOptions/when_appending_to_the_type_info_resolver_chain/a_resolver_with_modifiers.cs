// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain;

/// <summary>
/// Before Arc shipped source-generated metadata, the chain of its options was empty, so a resolver an application
/// appended after Arc configured them was the only one, and its modifiers applied to every type - Arc's own included.
/// Appending one has to keep doing exactly that.
/// </summary>
public class a_resolver_with_modifiers : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _optionsAsBefore;
    string _json;
    string _jsonAsBefore;

    void Establish()
    {
        _arcOptions = new ArcOptions();
        _optionsAsBefore = representative_wire_values.OptionsAsBefore();
        _optionsAsBefore.TypeInfoResolver = renaming_resolvers.RenamingQueryResultData("payload");
    }

    void Because()
    {
        _arcOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(renaming_resolvers.RenamingQueryResultData("payload"));
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, _arcOptions.JsonSerializerOptions);
        _jsonAsBefore = JsonSerializer.Serialize(representative_wire_values.QueryResult, _optionsAsBefore);
    }

    [Fact] void should_apply_the_modifiers() => _json.ShouldContain("\"payload\":");
    [Fact] void should_serialize_as_before() => _json.ShouldEqual(_jsonAsBefore);
}
