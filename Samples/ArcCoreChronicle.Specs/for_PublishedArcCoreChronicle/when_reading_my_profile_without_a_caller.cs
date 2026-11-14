// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Samples.ArcCoreChronicle.Specs.for_PublishedArcCoreChronicle;

public class when_reading_my_profile_without_a_caller : given.a_published_host_with_a_profile
{
    async Task Because() => await Post("read-mine", "{}");

    [PublishedArcCoreChronicleFact] void should_not_be_authorized() => Assert.False(_body.GetProperty("isAuthorized").GetBoolean(), Diagnostics);
    [PublishedArcCoreChronicleFact] void should_respond_with_forbidden() => Assert.True(_statusCode == HttpStatusCode.Forbidden, Diagnostics);
    [PublishedArcCoreChronicleFact] void should_not_disclose_any_display_name() => Assert.False(_responseText.Contains(DisplayName, StringComparison.Ordinal), Diagnostics);
}
