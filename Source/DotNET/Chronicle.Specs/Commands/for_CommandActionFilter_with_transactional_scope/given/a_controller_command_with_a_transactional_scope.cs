// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;
using Cratis.Arc.Commands;
using Cratis.Traces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.given;

public class a_controller_command_with_a_transactional_scope : a_transactional_command_scope
{
    protected CommandActionFilter _filter;
    protected ActionExecutingContext _executing;
    protected ActionExecutedContext _executed;
    ActivitySource _activity;

    protected CommandResult<object> Result => (CommandResult<object>)((ObjectResult)_executed.Result!).Value!;

    void Establish()
    {
        var scopes = Substitute.For<IInstancesOf<ICommandExecutionScope>>();
        scopes.GetEnumerator().Returns(_ => new ICommandExecutionScope[] { _scope }.AsEnumerable().GetEnumerator());
        _serviceProvider.GetService(typeof(IInstancesOf<ICommandExecutionScope>)).Returns(scopes);
        var http = new DefaultHttpContext { RequestServices = _serviceProvider };
        http.Request.Method = HttpMethod.Post.Method;
        var command = new ControllerCommand("value");
        var descriptor = new ControllerActionDescriptor
        {
            ControllerName = "TestController",
            ActionName = "Execute",
            Parameters =
            [
                new ControllerParameterDescriptor
                {
                    Name = "command",
                    ParameterType = typeof(ControllerCommand),
                    BindingInfo = new BindingInfo { BindingSource = BindingSource.Body }
                }
            ]
        };
        var action = new ActionContext(http, new RouteData(), descriptor, new ModelStateDictionary());
        _executing = new(action, [], new Dictionary<string, object?> { { "command", command } }, new object());
        _executed = new(action, [], new object()) { Result = new ObjectResult("response") };
        var activitySource = Substitute.For<IActivitySource<CommandActionFilter>>();
        _activity = new("ControllerScopeSpecs");
        activitySource.ActualSource.Returns(_activity);
        var valuesBuilder = Substitute.For<ICommandContextValuesBuilder>();
        valuesBuilder.Build(Arg.Any<object>()).Returns(_ => new CommandContextValues());
        _filter = new(Substitute.For<ICommandContextModifier>(), valuesBuilder, activitySource);
    }

    void Cleanup() => _activity.Dispose();

    record ControllerCommand(string Value);
}
