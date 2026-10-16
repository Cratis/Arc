// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unadmitted_date_conditions : a_generated_document
{
    [Theory]
    [InlineData("DateOnly?", "End >= Start", "optional")]
    [InlineData("DateOnly", "End >= DateOnly.FromDateTime(DateTime.Today)", "clock")]
    [InlineData("DateTime", "End >= DateTime.Now", "clock")]
    [InlineData("DateTimeOffset", "End >= DateTimeOffset.UtcNow", "clock")]
    [InlineData("DateTimeOffset", "End >= DateTimeOffset.Parse(\"2026-01-01T00:00:00\", CultureInfo.InvariantCulture)", "offset")]
    [InlineData("DateOnly", "End >= DateOnly.Parse(\"not-a-date\", CultureInfo.InvariantCulture)", "calendar date")]
    public void should_report_the_property_and_refusal_without_emitting_an_invalid_condition(string type, string comparison, string reason)
    {
        Generate((Analyzed.SlicePath, DateComparisonSources.Guard(type, comparison)));
        Result.Source.ShouldNotContain("when end");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction &&
            diagnostic.Message.Contains("End", StringComparison.Ordinal) && diagnostic.Message.Contains(reason, StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
