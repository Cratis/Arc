// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Samples.ArcCoreChronicle.Specs.for_PublishedArcCoreChronicle;

public class when_reading_a_missing_protected_profile : given.a_published_host_with_a_profile
{
    async Task Because() => await Post("read-by-key", /*lang=json,strict*/ """{"subject":"subject-that-was-never-registered"}""");

    [PublishedArcCoreChronicleFact] void should_respond_with_bad_request() => Assert.True(_statusCode == HttpStatusCode.BadRequest, Diagnostics);
    [PublishedArcCoreChronicleFact] void should_not_be_valid() => Assert.False(_body.GetProperty("isValid").GetBoolean(), Diagnostics);
    [PublishedArcCoreChronicleFact] void should_report_that_the_profile_does_not_exist() => Assert.True(_body.GetProperty("validationResults").EnumerateArray().Any(result => result.GetProperty("message").GetString()!.Contains("does not exist", StringComparison.Ordinal)), Diagnostics);
    [PublishedArcCoreChronicleFact] void should_not_disclose_any_display_name() => Assert.False(_responseText.Contains(DisplayName, StringComparison.Ordinal), Diagnostics);
}
