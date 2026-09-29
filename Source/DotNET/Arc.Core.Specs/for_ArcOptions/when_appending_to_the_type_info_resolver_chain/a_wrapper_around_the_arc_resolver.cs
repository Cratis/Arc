// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain;

/// <summary>
/// An appended resolver that wraps the options' own resolver with a modifier holds Arc's resolver. When Arc's resolver
/// is consulted again from within it, it resolves the type itself, so the modifier applies to what it resolves.
/// </summary>
public class a_wrapper_around_the_arc_resolver : Specification
{
    ArcOptions _arcOptions;
    string _json;

    void Establish() => _arcOptions = new ArcOptions();

    void Because()
    {
        var options = _arcOptions.JsonSerializerOptions;
        options.TypeInfoResolverChain.Add(options.TypeInfoResolver!.WithAddedModifier(renaming_resolvers.RenameQueryResultData("payload")));
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
    }

    [Fact] void should_apply_the_modifier() => _json.ShouldContain("\"payload\":");
}
