// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_serializing_arc_wire_types;

/// <summary>
/// With reflection-based serialization disabled, as in a trimmed or NativeAOT application, Arc's own metadata is all
/// its resolver has left; it has to cover Arc's wire types on its own and write them as reflection did.
/// </summary>
public class without_reflection : Specification
{
    JsonSerializerOptions _options;
    JsonSerializerOptions _optionsAsBefore;
    QueryResult _queryResult;
    CommandResult _commandResult;

    void Establish()
    {
        _options = new JsonSerializerOptions().ConfigureArcDefaults();
        _options.TypeInfoResolverChain[0] = new ArcDefaultsJsonTypeInfoResolver(_options, reflectionResolver: null);
        _optionsAsBefore = representative_wire_values.OptionsAsBefore();

        _queryResult = representative_wire_values.QueryResult;
        _queryResult.Data = "text";
        _queryResult.ValidationResults = [];
        _queryResult.ChangeSet = new ChangeSet { Added = ["added"], Removed = [1] };
        _commandResult = representative_wire_values.CommandResult;
    }

    [Fact] void should_serialize_a_query_result_the_same() =>
        JsonSerializer.Serialize(_queryResult, _options).ShouldEqual(JsonSerializer.Serialize(_queryResult, _optionsAsBefore));

    [Fact] void should_serialize_a_command_result_the_same() =>
        JsonSerializer.Serialize(_commandResult, _options).ShouldEqual(JsonSerializer.Serialize(_commandResult, _optionsAsBefore));

    [Fact] void should_serialize_a_hub_message_the_same() =>
        JsonSerializer.Serialize(ObservableQueryHubMessage.CreateQueryResult("query-1", _queryResult, 2), _options)
            .ShouldEqual(JsonSerializer.Serialize(ObservableQueryHubMessage.CreateQueryResult("query-1", _queryResult, 2), _optionsAsBefore));
}
