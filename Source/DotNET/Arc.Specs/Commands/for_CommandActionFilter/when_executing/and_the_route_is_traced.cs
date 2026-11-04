// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_route_is_traced : given.a_post_request_for_a_command_action
{
    readonly ActivitySource _source = new("Cratis.Arc.ActionTest");
    ActivityListener _listener;
    Activity _span;

    void Establish()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, _source),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _span = activity
        };
        ActivitySource.AddActivityListener(_listener);
        var source = Substitute.For<IActivitySource<CommandActionFilter>>();
        source.ActualSource.Returns(_source);
        _filter = new CommandActionFilter(_contextModifier, _contextValuesBuilder, source);
        _actionContext.ActionDescriptor.AttributeRouteInfo = new AttributeRouteInfo { Template = "orders/{id}" };
        _actionContext.HttpContext.Request.Path = "/orders/private-123";
    }

    Task Because() => _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    void Destroy()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    [Fact] void should_keep_the_span_name() => _span.DisplayName.ShouldEqual("cratis.arc.command.action");
    [Fact] void should_keep_the_server_kind() => _span.Kind.ShouldEqual(ActivityKind.Server);
    [Fact] void should_keep_the_legacy_route_template() => _span.GetTagItem("route_template").ShouldEqual("orders/{id}");
    [Fact] void should_add_the_aligned_route_template() => _span.GetTagItem(WellKnownTelemetryNames.HttpRoute).ShouldEqual("orders/{id}");
    [Fact] void should_not_record_the_concrete_path() => _span.TagObjects.Any(tag => tag.Value?.ToString()?.Contains("private-123", StringComparison.Ordinal) == true).ShouldBeFalse();
}
