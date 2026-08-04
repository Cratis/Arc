// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

/// <summary>
/// The two halves of how a type crosses the wire, at the one point they meet. The built-in map carries a default
/// chosen to be right without configuration - a <see cref="DateOnly"/> is a calendar date, so it crosses as its
/// ISO-8601 string rather than as a JavaScript <c>Date</c> that would invent an instant for it. The mapping seam
/// then lets a consumer who wants something richer say so.
/// </summary>
/// <remarks>
/// Specified because the two were built independently and their composition is the part a consumer relies on:
/// the default has to be usable by someone who configures nothing, and it has to be replaceable by someone who
/// wants a real calendar-date type. A default that could not be overridden would force a fork; a seam that had to
/// be configured before the common case was correct would be a wrong default with extra steps.
/// </remarks>
public class when_a_type_mapping_replaces_the_temporal_default : given.no_type_mappings
{
    TargetType _dateOnlyByDefault = null!;
    TargetType _timeOnlyByDefault = null!;
    TargetType _dateOnlyMapped = null!;

    void Establish()
    {
        _dateOnlyByDefault = typeof(DateOnly).GetTargetType();
        _timeOnlyByDefault = typeof(TimeOnly).GetTargetType();
        TypeExtensions.SetTypeMappings([(typeof(DateOnly).FullName!, "LocalDate", "@acme/time")]);
    }

    void Because() => _dateOnlyMapped = typeof(DateOnly).GetTargetType();

    [Fact] void should_default_a_calendar_date_to_its_string_form() => _dateOnlyByDefault.Type.ShouldEqual("string");
    [Fact] void should_default_a_time_of_day_to_its_string_form() => _timeOnlyByDefault.Type.ShouldEqual("string");
    [Fact] void should_emit_no_import_for_the_default() => _dateOnlyByDefault.Module.ShouldBeEmpty();

    [Fact] void should_let_a_consumer_replace_the_default() => _dateOnlyMapped.Type.ShouldEqual("LocalDate");
    [Fact] void should_import_the_replacement_from_its_package() => _dateOnlyMapped.Module.ShouldEqual("@acme/time");

    /// <summary>
    /// A type the consumer said nothing about keeps the default, so replacing one temporal type does not quietly
    /// move the other.
    /// </summary>
    [Fact] void should_leave_an_unmapped_sibling_alone() => typeof(TimeOnly).GetTargetType().Type.ShouldEqual("string");
}
