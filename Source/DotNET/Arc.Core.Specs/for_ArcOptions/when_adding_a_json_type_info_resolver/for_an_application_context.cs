// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver;

/// <summary>
/// An application chains its own source-generated context in front of Arc's metadata, so its read models serialize
/// without reflection - and exactly as they did through reflection.
/// </summary>
public class for_an_application_context : Specification
{
    ArcOptions _arcOptions;
    IJsonTypeInfoResolver _otherResolver;
    ArcOptions _returned;
    JsonSerializerOptions _withoutReflection;
    JsonSerializerOptions _optionsAsBefore;
    string _jsonWithoutReflection;
    string _jsonAsBefore;

    void Establish()
    {
        _arcOptions = new ArcOptions();
        _otherResolver = Substitute.For<IJsonTypeInfoResolver>();
    }

    void Because()
    {
        _returned = _arcOptions
            .AddJsonTypeInfoResolver(an_application_json_serializer_context.Default)
            .AddJsonTypeInfoResolver(_otherResolver)
            .AddJsonTypeInfoResolver(an_application_json_serializer_context.Default);

        // What a trimmed or NativeAOT application is left with: reflection-based serialization is disabled.
        _withoutReflection = new JsonSerializerOptions(_arcOptions.JsonSerializerOptions);
        _withoutReflection.TypeInfoResolverChain[^1] = new ArcDefaultsJsonTypeInfoResolver(_withoutReflection, reflectionResolver: null);
        _optionsAsBefore = representative_wire_values.OptionsAsBefore();
        _jsonWithoutReflection = JsonSerializer.Serialize(representative_wire_values.QueryResult, _withoutReflection);
        _jsonAsBefore = JsonSerializer.Serialize(representative_wire_values.QueryResult, _optionsAsBefore);
    }

    [Fact] void should_return_the_options_for_continuation() => _returned.ShouldEqual(_arcOptions);
    [Fact] void should_put_the_application_context_first() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[0].ShouldEqual(an_application_json_serializer_context.Default);
    [Fact] void should_keep_the_order_resolvers_were_added_in() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[1].ShouldEqual(_otherResolver);
    [Fact] void should_end_with_arc_resolver() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain[2].ShouldBeOfExactType<ArcDefaultsJsonTypeInfoResolver>();
    [Fact] void should_not_add_a_resolver_twice() => _arcOptions.JsonSerializerOptions.TypeInfoResolverChain.Count.ShouldEqual(3);
    [Fact] void should_serialize_the_application_types_without_reflection_as_before() => _jsonWithoutReflection.ShouldEqual(_jsonAsBefore);
}
