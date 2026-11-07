// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting.given;

public class startup_capability_endpoints
{
    public int SelectedCalls { get; private set; }

    public string Selected()
    {
        SelectedCalls++;
        return "selected";
    }
}
