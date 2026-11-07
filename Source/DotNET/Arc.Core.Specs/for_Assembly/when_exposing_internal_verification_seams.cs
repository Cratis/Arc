// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.for_Assembly;

public class when_exposing_internal_verification_seams : Specification
{
    bool _dynamicProxiesAreFriends;

    void Because() => _dynamicProxiesAreFriends = typeof(IVerifiedCredentialSource).Assembly
        .GetCustomAttributes<InternalsVisibleToAttribute>()
        .Any(attribute => attribute.AssemblyName.Split(',')[0] == "DynamicProxyGenAssembly2");

    [Fact] void should_not_grant_dynamic_proxies_access() => _dynamicProxiesAreFriends.ShouldBeFalse();
}
