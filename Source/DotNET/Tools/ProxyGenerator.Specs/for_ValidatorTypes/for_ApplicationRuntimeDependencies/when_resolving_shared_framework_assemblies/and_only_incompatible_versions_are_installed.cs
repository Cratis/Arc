// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_only_incompatible_versions_are_installed : given.installed_shared_frameworks
{
    void Establish()
    {
        Install("10.0.12");
        Install("12.0.1");
        Install("11.1.1");
        Install("not-a-version");
    }

    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_leave_the_assembly_unresolved() => ResolvedPath.ShouldBeNull();
}
