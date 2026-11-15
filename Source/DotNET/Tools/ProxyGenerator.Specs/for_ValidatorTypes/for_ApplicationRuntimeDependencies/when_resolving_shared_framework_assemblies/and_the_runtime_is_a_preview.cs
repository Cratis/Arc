// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_the_runtime_is_a_preview : given.installed_shared_frameworks
{
    string _matchingPath = null!;

    void Establish()
    {
        RuntimeDirectory = Path.Combine(Path.GetDirectoryName(RuntimeDirectory)!, "11.0.0-preview.7");
        _matchingPath = Install("11.0.0-preview.7");
        Install("11.0.0-preview.8");
    }

    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_use_the_matching_preview() => ResolvedPath.ShouldEqual(_matchingPath);
}
