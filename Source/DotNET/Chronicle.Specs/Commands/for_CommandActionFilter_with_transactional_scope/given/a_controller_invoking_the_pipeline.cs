// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Subjects;
using Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.given;

public class a_controller_invoking_the_pipeline : a_read_protection_pipeline
{
    protected UnitOfWorkManager _manager;
    protected IUnitOfWork _controllerUnit;
    protected IUnitOfWork _innerUnit;
    protected CommandResult _innerResult;
    protected List<object> _committed;
    protected Func<Task> _action;
    protected Func<IEnumerable<EventForEventSourceId>, AppendManyResult> _append;
    protected Subject<IEnumerable<AppendedEventWithResult>> _appends;
    protected CorrelationId Correlation => _unit.CorrelationId;
    protected CommandResult<object> ControllerResult => (CommandResult<object>)((ObjectResult)(_executing.Result ?? _executed.Result)!).Value!;
    CommandActionFilter _filter;
    ActionExecutingContext _executing;
    ActionExecutedContext _executed;
    ActivitySource _activity;

    void Establish()
    {
        _manager = new(_store);
        _services.GetService(typeof(IUnitOfWorkManager)).Returns(_manager);
        _committed = [];
        _appends = new();
        var log = (IEventLog)_store.GetEventSequence(EventSequenceId.Log);
        log.Id.Returns(EventSequenceId.Log);
        log.AppendOperations.Returns(_appends);
        _services.GetService(typeof(IEventLog)).Returns(log);
        _append = events =>
        {
            _committed.AddRange(events.Select(_ => _.Event));
            return AppendManyResult.Success(Correlation, [EventSequenceNumber.First]);
        };
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call => _append(call.Arg<IEnumerable<EventForEventSourceId>>()));
        var scopes = new KnownInstancesOf<ICommandExecutionScope>([_scope]);
        _services.GetService(typeof(IInstancesOf<ICommandExecutionScope>)).Returns(scopes);
        var http = new DefaultHttpContext { RequestServices = _services };
        http.Request.Method = HttpMethod.Post.Method;
        http.Request.Headers[Cratis.Arc.Execution.Constants.DefaultCorrelationIdHeader] = Correlation.ToString();
        var descriptor = new ControllerActionDescriptor { ControllerName = "Test", ActionName = "Execute" };
        var context = new ActionContext(http, new RouteData(), descriptor, new ModelStateDictionary());
        _executing = new(context, [], new Dictionary<string, object?>(), new object());
        _executed = new(context, [], new object()) { Result = new ObjectResult("response") };
        var activity = Substitute.For<IActivitySource<CommandActionFilter>>();
        _activity = new("ControllerPipelineSpecs");
        activity.ActualSource.Returns(_activity);
        var builder = Substitute.For<ICommandContextValuesBuilder>();
        builder.Build(Arg.Any<object>()).Returns(_ => new CommandContextValues());
        _filter = new(Substitute.For<ICommandContextModifier>(), builder, activity);
        _handle = _ =>
        {
            _innerUnit = _manager.Current;
            Stage("inner");
            return Task.FromResult<object?>(null);
        };
        _action = async () => _innerResult = await Execute(new OffCommand());
    }

    protected void Stage(string value) => _manager.Current.AddEvent(EventSequenceId.Log, EventSourceId.New(), value, Causation.Unknown());

    protected Task Invoke() => _filter.OnActionExecutionAsync(_executing, async () =>
    {
        _controllerUnit = _manager.Current;
        await _action();
        return _executed;
    });

    void Destroy()
    {
        _activity.Dispose();
        _appends.Dispose();
    }
}
