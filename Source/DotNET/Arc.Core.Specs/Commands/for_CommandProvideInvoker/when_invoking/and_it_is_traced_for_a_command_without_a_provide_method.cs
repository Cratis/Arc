// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_it_is_traced_for_a_command_without_a_provide_method : given.a_command_provide_invoker
{
    public record RegisterAuthor(string Name);

    ActivitySource _source;
    TelemetryRecorder _telemetry;

    void Establish()
    {
        _source = new ActivitySource("Cratis.Arc.Test");
        var activitySource = Substitute.For<IActivitySource<CommandProvideInvoker>>();
        activitySource.ActualSource.Returns(_source);
        _serviceProvider.GetService(typeof(IActivitySource<CommandProvideInvoker>)).Returns(activitySource);
        _telemetry = new TelemetryRecorder(_source);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _source.Dispose();
    }

    async Task Because() => await Invoke(new RegisterAuthor("a name"));

    [Fact] void should_not_raise_a_span() => _telemetry.Activities.ShouldBeEmpty();
}
