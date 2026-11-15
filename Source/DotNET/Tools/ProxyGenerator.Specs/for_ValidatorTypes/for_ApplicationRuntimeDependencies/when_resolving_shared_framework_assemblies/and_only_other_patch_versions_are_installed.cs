// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_only_other_patch_versions_are_installed : given.installed_shared_frameworks
{
    string _latestPath = null!;

    void Establish()
    {
        Install("11.0.9");
        _latestPath = Install("11.0.12");
        Install("12.0.1");
        Install("11.1.1");
    }

    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_use_the_latest_compatible_patch() => ResolvedPath.ShouldEqual(_latestPath);
}
