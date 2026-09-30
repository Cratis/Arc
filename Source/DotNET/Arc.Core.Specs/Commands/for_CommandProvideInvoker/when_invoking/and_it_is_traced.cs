// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_it_is_traced : given.a_command_provide_invoker
{
    public class RegisterAuthor
    {
        public string Provide() => "the value";
    }

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

    async Task Because() => await Invoke(new RegisterAuthor());

    Activity ProvideSpan => _telemetry.Span("cratis.arc.command.provide");

    [Fact] void should_name_the_span_after_the_command() => ProvideSpan.DisplayName.ShouldEqual($"{nameof(RegisterAuthor)}.Provide()");
    [Fact] void should_add_the_command_type() => ProvideSpan.GetTagItem("cratis.arc.command.type").ShouldEqual(typeof(RegisterAuthor).FullName);
}
