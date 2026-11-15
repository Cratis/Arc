// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_aspnetcore_is_not_installed : given.installed_shared_frameworks
{
    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_leave_the_assembly_unresolved() => ResolvedPath.ShouldBeNull();
}
