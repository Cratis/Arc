// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// A <see cref="DateOnly"/> is a calendar date and a <see cref="TimeOnly"/> is a time of day; neither denotes an
/// instant. Emitting them as a JavaScript <c>Date</c> invents one - the wire value <c>"2026-05-12"</c> becomes
/// <c>2026-05-12T00:00:00.000Z</c>, and every browser-local getter west of UTC then reads it back as the previous
/// day. Both cross the wire as their ISO-8601 string, and each has a type of its own in <c>@cratis/fundamentals</c>
/// that holds exactly that - so the generated proxy names the type rather than coercing the value into an instant,
/// and stays distinguishable both from a plain string and from the two instant types that legitimately are dates.
/// </summary>
public class when_generating_type_with_temporal_properties : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    TypeDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();
        _descriptor = typeof(TypeWithTemporalProperties).ToTypeDescriptor(string.Empty, 0);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateType(_descriptor);

        try
        {
            var transpiledCode = _runtime.TranspileTypeScript(_generatedCode);
            _typeScriptIsValid = !string.IsNullOrEmpty(transpiledCode);
        }
        catch
        {
            _typeScriptIsValid = false;
        }
    }

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();

    [Fact] void should_emit_date_time_as_an_instant() => _generatedCode.ShouldContain("@field(Date)\n    dateTimeValue!: Date;");
    [Fact] void should_emit_date_time_offset_as_an_instant() => _generatedCode.ShouldContain("@field(Date)\n    dateTimeOffsetValue!: Date;");

    [Fact] void should_not_coerce_a_calendar_date_into_an_instant() => _generatedCode.ShouldContain("@field(DateOnly)\n    dateOnlyValue!: DateOnly;");
    [Fact] void should_not_coerce_a_time_of_day_into_an_instant() => _generatedCode.ShouldContain("@field(TimeOnly)\n    timeOnlyValue!: TimeOnly;");
    [Fact] void should_import_the_calendar_date_type() => _generatedCode.ShouldContain("import { DateOnly } from '@cratis/fundamentals'");
    [Fact] void should_import_the_time_of_day_type() => _generatedCode.ShouldContain("import { TimeOnly } from '@cratis/fundamentals'");

    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
