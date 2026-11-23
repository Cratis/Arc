// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_serializing;

public class mapped_results : given.an_exception_response_mapping
{
    string[] _json;

    void Because()
    {
        Map();
        var generic = new CommandResult<string>();
        generic.MergeWith(_command);
        _json = [
            JsonSerializer.Serialize(_command, ResultJsonContext.Default.CommandResult),
            JsonSerializer.Serialize(generic, ResultJsonContext.Default.CommandResultString),
            JsonSerializer.Serialize(_query, ResultJsonContext.Default.QueryResult)];
    }

    [Fact] void should_include_only_the_public_code() => _json.All(json => json.Contains("\"errorCode\":\"namespace_closing\"")).ShouldBeTrue();
    [Fact] void should_not_serialize_private_detail() => _json.Any(json => json.Contains("private")).ShouldBeFalse();
    [Fact] void should_not_serialize_the_exception_identity() => _json.Any(json => json.Contains("exceptionResponseCandidate")).ShouldBeFalse();
    [Fact] void should_not_serialize_the_mapped_status() => _json.Any(json => json.Contains("statusCode")).ShouldBeFalse();
    [Fact] void should_not_serialize_the_headers() => _json.Any(json => json.Contains("Retry-After")).ShouldBeFalse();
}
