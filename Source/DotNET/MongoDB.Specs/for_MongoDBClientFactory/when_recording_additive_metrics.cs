// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory;

public class when_recording_additive_metrics : Specification
{
    readonly System.Diagnostics.Metrics.Meter _legacy = new("Cratis.Arc");
    readonly System.Diagnostics.Metrics.Meter _aligned = new(WellKnownMongoDBDiagnostics.MeterName);
    readonly List<string> _legacyNames = [];
    readonly Dictionary<string, int> _alignedValues = [];
    MeterListener _listener;

    void Establish()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, _legacy)) _legacyNames.Add(instrument.Name);
                if (ReferenceEquals(instrument.Meter, _aligned)) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, _, _) => _alignedValues[instrument.Name] = value);
        _listener.Start();
    }

    void Because()
    {
        var oldScope = new Cratis.Metrics.Meter<IMongoClient>(_legacy).BeginScope("test-server");
        var newScope = new Cratis.Metrics.Meter<MongoDBClientFactory>(_aligned).BeginScope(new Dictionary<string, object> { ["Server"] = "test-server" });
        oldScope.OpenConnections(7);
        oldScope.CheckedOutConnections(3);
        oldScope.ConnectionsAddedToPool(4);
        oldScope.Commands(2);
        oldScope.FailedConnections();
        oldScope.AggregatedCommands();
        newScope.OpenConnections(7);
        newScope.CheckedOutConnections(3);
        newScope.ConnectionsAddedToPool(4);
        newScope.Commands(2);
        newScope.FailedConnections();
        newScope.AggregatedCommands();
        _listener.RecordObservableInstruments();
    }

    void Destroy()
    {
        _listener.Dispose();
        _aligned.Dispose();
        _legacy.Dispose();
    }

    [Fact] void should_have_a_separate_meter() => _aligned.Name.ShouldEqual("Cratis.Arc.MongoDB");
    [Fact] void should_keep_the_legacy_instruments() => _legacyNames.ShouldContainOnly(["mongodb-open-connections", "mongodb-open-connections", "mongodb-connections-in-pool", "mongodb-commands", "mongodb-failed-connections", "mongodb-aggregated-commands"]);
    [Fact] void should_have_six_distinct_dotted_instruments() => _alignedValues.Count.ShouldEqual(6);
    [Fact] void should_record_open_connections() => _alignedValues["mongodb.open.connections"].ShouldEqual(7);
    [Fact] void should_record_checked_out_connections_separately() => _alignedValues["mongodb.checked.out.connections"].ShouldEqual(3);
    [Fact] void should_record_pooled_connections() => _alignedValues["mongodb.connections.in.pool"].ShouldEqual(4);
    [Fact] void should_record_active_commands() => _alignedValues["mongodb.commands"].ShouldEqual(2);
    [Fact] void should_record_failed_connections() => _alignedValues["mongodb.failed.connections"].ShouldEqual(1);
    [Fact] void should_record_command_starts() => _alignedValues["mongodb.aggregated.commands"].ShouldEqual(1);
}
