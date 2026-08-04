// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// The behavioral half of <see cref="when_generating_type_with_temporal_properties"/>: what the emitted decorator
/// costs at runtime. The server sends a <see cref="DateOnly"/> as the ISO string <c>"2026-05-12"</c>. Deserialized
/// through a proxy that declares the field a <c>Date</c>, that string becomes the instant
/// <c>2026-05-12T00:00:00.000Z</c> - and every browser-local getter west of UTC then reports the 11th. The value
/// must survive the round trip as the calendar date the server sent, with no instant invented for it.
/// </summary>
public class when_deserializing_temporal_properties_through_the_generated_proxy : Specification, IDisposable
{
    const string DateOnlyOnTheWire = "2026-05-12";
    const string TimeOnlyOnTheWire = "14:30:45";

    JavaScriptRuntime _runtime = null!;
    string _deserializedDateOnly = null!;
    string _deserializedTimeOnly = null!;
    string _localCalendarDayWestOfUtc = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var generatedCode = InMemoryProxyGenerator.GenerateType(typeof(TypeWithTemporalProperties).ToTypeDescriptor(string.Empty, 0));
        _runtime.Execute(_runtime.TranspileTypeScript(generatedCode));
        _runtime.Execute("globalThis.TypeWithTemporalProperties = exports.TypeWithTemporalProperties;");
    }

    void Because()
    {
        _runtime.Execute(
            "globalThis.__deserialized = JsonSerializer.deserializeFromInstance(globalThis.TypeWithTemporalProperties, " +
            $"{{ dateOnlyValue: '{DateOnlyOnTheWire}', timeOnlyValue: '{TimeOnlyOnTheWire}' }});");

        _deserializedDateOnly = _runtime.Evaluate<string>("String(globalThis.__deserialized.dateOnlyValue)")!;
        _deserializedTimeOnly = _runtime.Evaluate<string>("String(globalThis.__deserialized.timeOnlyValue)")!;

        // What a component west of UTC renders. A string has no zone to shift; a Date does, and this is where the
        // off-by-one becomes visible to a user.
        _localCalendarDayWestOfUtc = _runtime.Evaluate<string>(
            "(() => { const value = globalThis.__deserialized.dateOnlyValue; " +
            "return value instanceof Date " +
            "? new Intl.DateTimeFormat('en-CA', { timeZone: 'America/New_York' }).format(value) " +
            ": String(value); })()")!;
    }

    [Fact] void should_hand_back_the_calendar_date_the_server_sent() => _deserializedDateOnly.ShouldEqual(DateOnlyOnTheWire);
    [Fact] void should_hand_back_the_time_of_day_the_server_sent() => _deserializedTimeOnly.ShouldEqual(TimeOnlyOnTheWire);
    [Fact] void should_not_shift_the_calendar_day_west_of_utc() => _localCalendarDayWestOfUtc.ShouldEqual(DateOnlyOnTheWire);

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
