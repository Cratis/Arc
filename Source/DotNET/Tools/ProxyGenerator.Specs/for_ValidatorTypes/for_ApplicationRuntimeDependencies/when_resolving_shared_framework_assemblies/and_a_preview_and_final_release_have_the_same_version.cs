// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_a_preview_and_final_release_have_the_same_version : given.installed_shared_frameworks
{
    string _finalPath = null!;

    void Establish()
    {
        Install("11.0.0-preview.7");
        _finalPath = Install("11.0.0");
    }

    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_prefer_the_final_release() => ResolvedPath.ShouldEqual(_finalPath);
}
