// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_assigning_the_type_info_resolver;

/// <summary>
/// Arc's options always carry a resolver, so <c>TypeInfoResolver?.WithAddedModifier(...)</c> takes effect.
/// </summary>
public class with_null_conditional_modifier : Specification
{
    ArcOptions _arcOptions;
    string _json;

    void Establish() => _arcOptions = new ArcOptions();

    void Because()
    {
        var options = _arcOptions.JsonSerializerOptions;
        options.TypeInfoResolver = options.TypeInfoResolver?.WithAddedModifier(renaming_resolvers.RenameQueryResultData("payload"));
        _json = JsonSerializer.Serialize(representative_wire_values.QueryResult, options);
    }

    [Fact] void should_apply_the_modifier() => _json.ShouldContain("\"payload\":");
}
