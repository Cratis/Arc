// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_serializing;

public class unmapped_results : given.an_exception_response_mapping
{
    string[] _json;

    void Because() => _json = [
        JsonSerializer.Serialize(_command, ResultJsonContext.Default.CommandResult),
        JsonSerializer.Serialize(_query, ResultJsonContext.Default.QueryResult)];

    [Fact] void should_not_change_the_unmapped_wire_shape() => _json.Any(json => json.Contains("errorCode")).ShouldBeFalse();
    [Fact] void should_not_serialize_the_exception_identity() => _json.Any(json => json.Contains("exceptionResponseCandidate")).ShouldBeFalse();
}
