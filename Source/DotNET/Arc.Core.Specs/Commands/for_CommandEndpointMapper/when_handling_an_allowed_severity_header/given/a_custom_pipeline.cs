// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_handling_an_allowed_severity_header.given;

public class a_custom_pipeline : Specification
{
    protected ICommandPipeline _pipeline;
    protected Type _commandType;
    protected ArcOptions _options;
    protected List<ValidationResultSeverity?> _severities;
    protected string _header = "3";

    void Establish()
    {
        _commandType = typeof(NoPolicyCommand);
        _options = new ArcOptions { ExposeExceptionDetails = false };
        _severities = [];
        _pipeline = Substitute.For<ICommandPipeline>();
        _pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call =>
            {
                _severities.Add(call.ArgAt<ValidationResultSeverity?>(2));
                return new CommandResult();
            });
        _pipeline.Validate(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call =>
            {
                _severities.Add(call.ArgAt<ValidationResultSeverity?>(2));
                return new CommandResult();
            });
    }

    protected async Task HandleEndpoints()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(_commandType);
        handler.Location.Returns(["Features", "Commands"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var mapper = new a_recording_endpoint_mapper();
        mapper.MapCommandEndpoints(new ServiceCollection().AddSingleton(handlers).AddSingleton(Options.Create(_options)).BuildServiceProvider());
        var correlation = Substitute.For<ICorrelationIdAccessor>();
        correlation.Current.Returns(CorrelationId.New());
        var services = new ServiceCollection().AddLogging().AddSingleton(_pipeline).AddSingleton(Options.Create(_options)).AddSingleton(correlation)
            .AddSingleton(new AuthorizationDeclarations(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .BuildServiceProvider();
        foreach (var endpoint in mapper.Mapped)
        {
            var context = Substitute.For<IHttpRequestContext>();
            context.RequestServices.Returns(services);
            context.Headers.Returns(new Dictionary<string, string> { ["X-Allowed-Severity"] = _header });
            context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns(Activator.CreateInstance(_commandType));
            await endpoint.Handler(context);
        }
    }

    public record NoPolicyCommand;
}
