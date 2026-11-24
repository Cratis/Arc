// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.given;

public class an_exception_response_mapping : Specification
{
    protected IServiceProvider _services;
    protected IExceptionResponseMapper _mapper;
    protected ExceptionResponse _response;
    protected CommandResult _command;
    protected QueryResult _query;

    void Establish()
    {
        _services = Substitute.For<IServiceProvider>();
        _mapper = Substitute.For<IExceptionResponseMapper>();
        _response = new ExceptionResponse(HttpStatusCode.ServiceUnavailable, "namespace_closing", new Dictionary<string, string> { ["Retry-After"] = "5" });
        _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = _response;
            return true;
        });
        _services.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)).Returns(new KnownInstancesOf<IExceptionResponseMapper>([_mapper]));
        _command = CommandResult.FromException(CorrelationId.New(), new InvalidOperationException("private command detail"));
        _query = QueryResult.FromException(CorrelationId.New(), new InvalidOperationException("private query detail"));
    }

    protected void Map()
    {
        ExceptionResponseMapping.Apply(_command, _services, NullLogger.Instance);
        ExceptionResponseMapping.Apply(_query, _services, NullLogger.Instance);
    }
}
