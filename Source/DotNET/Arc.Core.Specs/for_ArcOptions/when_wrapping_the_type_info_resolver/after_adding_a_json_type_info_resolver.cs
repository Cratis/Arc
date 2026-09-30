// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver.given;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_wrapping_the_type_info_resolver;

/// <summary>
/// Adding a resolver through <see cref="ArcOptions.AddJsonTypeInfoResolver"/> also leaves the chain bound to the options
/// untouched, so wrapping the resolver afterwards keeps working.
/// </summary>
public class after_adding_a_json_type_info_resolver : Specification
{
    ArcOptions _arcOptions;
    bool _resolverIsTheBoundChain;
    string _json;

    void Establish() => _arcOptions = new ArcOptions().AddJsonTypeInfoResolver(an_application_json_serializer_context.Default);

    void Because()
    {
        var options = _arcOptions.JsonSerializerOptions;
        _resolverIsTheBoundChain = ReferenceEquals(options.TypeInfoResolver, options.TypeInfoResolverChain);
        options.TypeInfoResolver = options.TypeInfoResolver!.WithAddedModifier(renaming_resolvers.RenameQueryResultData("payload"));
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
    }

    [Fact] void should_not_carry_the_chain_bound_to_the_options() => _resolverIsTheBoundChain.ShouldBeFalse();
    [Fact] void should_apply_the_modifier() => _json.ShouldContain("\"payload\":");
}
