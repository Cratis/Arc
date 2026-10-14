// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_measure_observes_a_redirect : given.a_parameter_lookup
{
    MeterListener _listener;
    readonly List<string> _categories = [];
    ReadModelKey _result;

    void Establish()
    {
        Configure(new Convention(new("from")));
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Cratis.Arc.ReadProtection")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "category") _categories.Add((string)tag.Value!);
            }
        });
        _listener.Start();
    }

    void Because() => _result = CommandReadModelLookup.Resolve(_context, typeof(Convention).GetMethod(nameof(Convention.Provide))!.GetParameters()[0], ReadProtectionMode.Measure);
    void Destroy() => _listener.Dispose();
    [Fact] void should_keep_the_command_key() => _result.Value.ShouldEqual("subject");
    [Fact] void should_report_the_possible_redirect() => _categories.ShouldContain("redirect");
    [Fact] void should_not_read_another_model() => _readModels.ReceivedCalls().ShouldBeEmpty();
}
