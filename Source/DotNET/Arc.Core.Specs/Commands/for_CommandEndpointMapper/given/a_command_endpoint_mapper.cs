// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.given;

public class a_command_endpoint_mapper : Specification
{
    protected IHttpRequestContext _context;
    protected ILogger _logger;
    protected CorrelationId _correlationId;
    protected Type _commandType;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _logger = Substitute.For<ILogger>();
        _correlationId = CorrelationId.New();
        _commandType = typeof(SomeCommand);
    }

    protected static JsonException CaptureJsonException(string json)
    {
        try
        {
            JsonSerializer.Deserialize<SomeCommand>(json);
        }
        catch (JsonException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected the JSON to fail to deserialize, but it succeeded.");
    }
}
