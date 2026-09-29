// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain;

/// <summary>
/// An application context that knows only the application's types, appended to the chain of a trimmed or NativeAOT
/// application where reflection is disabled: it answers for the application's types, and Arc's metadata still answers
/// for Arc's own - where an appended context used to be the only resolver and failed for them.
/// </summary>
public class an_application_context_without_reflection : Specification
{
    JsonSerializerOptions _options;
    string _json;
    string _jsonAsBefore;

    void Establish()
    {
        _options = new JsonSerializerOptions().ConfigureArcDefaults();
        _options.TypeInfoResolverChain[0] = new ArcDefaultsJsonTypeInfoResolver(_options, reflectionResolver: null);
    }

    void Because()
    {
        _options.TypeInfoResolverChain.Add(an_application_json_serializer_context.Default);
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, _options);
        _jsonAsBefore = JsonSerializer.Serialize(representative_wire_values.QueryResult, representative_wire_values.OptionsAsBefore());
    }

    [Fact] void should_serialize_as_reflection_did() => _json.ShouldEqual(_jsonAsBefore);
}
