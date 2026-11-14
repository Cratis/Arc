// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The pure comparisons and settings parsing the read-protection measurement relies on.
/// </summary>
static class ReadProtectionMeasurementRules
{
    /// <summary>
    /// Determines whether a read would be protected through decision reads.
    /// </summary>
    /// <param name="description">The read's description.</param>
    /// <returns>True if admitted and not pinned-generation delivery; otherwise false.</returns>
    internal static bool IsProtectable(DecisionReadDescription description) =>
        description.Admission.IsAdmitted && !description.UsesPinnedGenerationDelivery;

    /// <summary>
    /// Determines whether a scope is narrower than the read's dimensions.
    /// </summary>
    /// <param name="read">The read.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>True if the scope narrows any dimension of the read; otherwise false.</returns>
    internal static bool IsNarrowerThan(MeasuredRead read, ConcurrencyScope scope) =>
        NarrowsStreamType(scope, read) || NarrowsStreamId(scope, read) || NarrowsSourceType(scope, read);

    /// <summary>
    /// Determines whether a scope starts after the captured boundary.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="boundary">The captured boundary, if any.</param>
    /// <returns>True if the scope's position is later than the boundary; otherwise false.</returns>
    internal static bool IsLaterThan(ConcurrencyScope scope, EventSequenceNumber? boundary) =>
        boundary is { } captured && scope.SequenceNumber.IsActualValue && (!captured.IsActualValue || scope.SequenceNumber > captured);

    /// <summary>
    /// Determines whether a scope would be refused under protection regardless of position.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns>True if the scope is unset, none or incomplete; otherwise false.</returns>
    internal static bool IsUnusable(ConcurrencyScope scope) =>
        scope == ConcurrencyScope.None || scope == ConcurrencyScope.NotSet || scope.IsIncomplete;

    /// <summary>
    /// Determines whether a scope would be refused under protection: narrower than the read, later than the boundary, or unusable.
    /// </summary>
    /// <param name="read">The read.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="boundary">The captured boundary, if any.</param>
    /// <returns>True if the scope would be refused; otherwise false.</returns>
    internal static bool WouldRefuse(MeasuredRead read, ConcurrencyScope scope, EventSequenceNumber? boundary) =>
        IsNarrowerThan(read, scope) || IsLaterThan(scope, boundary) || IsUnusable(scope);

    /// <summary>
    /// Reads a finite numeric setting.
    /// </summary>
    /// <param name="configuration">The configuration, if any.</param>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns>True if present, numeric and finite; otherwise false.</returns>
    internal static bool TryFinite(IConfiguration? configuration, string name, out double value) =>
        double.TryParse(ReadProtectionConfiguration.Value(configuration, name), out value) && double.IsFinite(value);

    /// <summary>
    /// Reads an integer setting.
    /// </summary>
    /// <param name="configuration">The configuration, if any.</param>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns>True if present and numeric; otherwise false.</returns>
    internal static bool TryInteger(IConfiguration? configuration, string name, out int value) =>
        int.TryParse(ReadProtectionConfiguration.Value(configuration, name), out value);

    /// <summary>
    /// Gets the name of a direct Chronicle or MongoDB read API a dependency type exposes.
    /// </summary>
    /// <param name="type">The dependency type.</param>
    /// <returns>The API name, or null when the type is not a read API.</returns>
    internal static string? ReadApiName(Type type)
    {
        if (typeof(IReadModels).IsAssignableFrom(type) || typeof(IEventStore).IsAssignableFrom(type) || typeof(IEventSequence).IsAssignableFrom(type))
        {
            return type.Name;
        }

        return type.IsGenericType && type.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1" ? "IMongoCollection" : null;
    }

    static bool NarrowsStreamType(ConcurrencyScope scope, MeasuredRead read) =>
        scope.EventStreamType is { IsAll: false } type && type != read.StreamType;

    static bool NarrowsStreamId(ConcurrencyScope scope, MeasuredRead read) =>
        scope.EventStreamId is { IsDefault: false } id && id != read.StreamId;

    static bool NarrowsSourceType(ConcurrencyScope scope, MeasuredRead read) =>
        scope.EventSourceType is { } sourceType &&
        sourceType != EventSourceType.Default &&
        sourceType != EventSourceType.Unspecified &&
        sourceType != read.SourceType;
}
