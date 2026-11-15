// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_the_runtime_version_is_installed : given.installed_shared_frameworks
{
    string _matchingPath = null!;

    void Establish()
    {
        _matchingPath = Install("11.0.1");
        Install("11.0.12");
    }

    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_prefer_the_active_runtime_version() => ResolvedPath.ShouldEqual(_matchingPath);
}
