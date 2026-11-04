// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Samples.ArcCore.Specs.Testing;

namespace Samples.ArcCore.Specs.for_PublishedArcCore.given;

public class a_published_arc_core : Specification
{
    protected PublishedArcCore _arcCore;

    protected virtual string HostEnvironment => "Development";

    async Task Establish()
    {
        if (Environment.GetEnvironmentVariable("CRATIS_ARC_PUBLISHED_ARCCORE") is not null)
        {
            _arcCore = await PublishedArcCore.Start(HostEnvironment);
        }
    }

    async Task Destroy()
    {
        if (_arcCore is not null)
        {
            await _arcCore.Stop();
        }
    }
}
