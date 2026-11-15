// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Samples.ArcCoreChronicle.Specs.for_PublishedArcCoreChronicle;

public class when_reading_a_protected_profile : given.a_published_host_with_a_profile
{
    async Task Because() => await ReadWhenProjected("read-by-key", $$"""{"subject":"{{_subject}}"}""");

    [PublishedArcCoreChronicleFact] void should_respond_with_ok() => Assert.True(_statusCode == HttpStatusCode.OK, Diagnostics);
    [PublishedArcCoreChronicleFact] void should_return_the_display_name() => Assert.True(_body.GetProperty("response").GetString() == DisplayName, Diagnostics);
}
