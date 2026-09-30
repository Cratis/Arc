// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_wrapping_the_type_info_resolver;

/// <summary>
/// Applications add a modifier by wrapping the resolver the options carry. On .NET 8 and .NET 9 a resolver that is the
/// chain bound to the options is cleared and refilled by that assignment, leaving a chain around itself that recurses
/// until the process dies, so Arc assigns a chain of its own instead of modifying the bound one.
/// </summary>
public class with_a_modifier : Specification
{
    ArcOptions _arcOptions;
    bool _resolverIsTheBoundChain;
    string _json;

    void Establish() => _arcOptions = new ArcOptions();

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
