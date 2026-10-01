// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.given;

public class a_traced_command_provide_invoker : a_command_provide_invoker
{
    protected ActivitySource _source;
    protected TelemetryRecorder _telemetry;

    protected Activity ProvideSpan => _telemetry.Span(WellKnownTelemetryNames.CommandProvideSpan);

    void Establish()
    {
        _source = new ActivitySource("Cratis.Arc.Test");
        var activitySource = Substitute.For<IActivitySource<CommandProvideInvoker>>();
        activitySource.ActualSource.Returns(_source);
        _invoker = new(activitySource);
        _telemetry = new TelemetryRecorder(_source);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _source.Dispose();
    }
}
