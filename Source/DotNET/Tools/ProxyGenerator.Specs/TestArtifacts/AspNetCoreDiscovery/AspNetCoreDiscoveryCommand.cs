// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Specs.AspNetCoreDiscovery;

[Command]
public record AspNetCoreDiscoveryCommand(string Value)
{
    public void Handle()
    {
    }
}
