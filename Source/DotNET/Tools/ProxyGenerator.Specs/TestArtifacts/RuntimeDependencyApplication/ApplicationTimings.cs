// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyLibrary;

namespace Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication;

public static class ApplicationTimings
{
    public static string Touch() => RuntimeDependencyMarker.Describe();
}
