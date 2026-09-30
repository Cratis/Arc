// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_assigning_the_type_info_resolver;

/// <summary>
/// Arc's options always carry a resolver, so <c>TypeInfoResolver ??= x</c> assigns nothing. Applications use
/// <see cref="ArcOptions.AddJsonTypeInfoResolver"/>, append to the chain or assign explicitly instead.
/// </summary>
public class with_null_coalescing : Specification
{
    ArcOptions _arcOptions;
    IJsonTypeInfoResolver _resolverBefore;
    string _json;
    string _jsonWithArcDefaults;

    void Establish() => _arcOptions = new ArcOptions();

    void Because()
    {
        var options = _arcOptions.JsonSerializerOptions;
        _resolverBefore = options.TypeInfoResolver;
        options.TypeInfoResolver ??= renaming_resolvers.RenamingQueryResultData("payload");
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
        _jsonWithArcDefaults = JsonSerializer.Serialize(representative_wire_values.QueryResult, new ArcOptions().JsonSerializerOptions);
    }

    [Fact] void should_already_carry_a_resolver() => _resolverBefore.ShouldNotBeNull();
    [Fact] void should_not_assign_anything() => _arcOptions.JsonSerializerOptions.TypeInfoResolver.ShouldEqual(_resolverBefore);
    [Fact] void should_not_apply_the_resolver() => _json.ShouldEqual(_jsonWithArcDefaults);
}
