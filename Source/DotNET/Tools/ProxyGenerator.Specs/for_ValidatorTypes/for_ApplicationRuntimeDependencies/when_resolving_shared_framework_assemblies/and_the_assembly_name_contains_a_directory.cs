// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.when_resolving_shared_framework_assemblies;

public class and_the_assembly_name_contains_a_directory : given.installed_shared_frameworks
{
    void Establish()
    {
        var installed = Install("11.0.1");
        var escaped = Path.Combine(Path.GetDirectoryName(installed)!, "..", "escaped.dll");
        File.WriteAllText(escaped, string.Empty);
        RequestedAssembly.Name = "../escaped";
    }

    void Because() => ResolvedPath = ApplicationRuntimeDependencies.SharedFrameworkAssemblyPath(RequestedAssembly, RuntimeDirectory);

    [Fact] void should_reject_the_assembly_name_even_when_the_file_exists() => ResolvedPath.ShouldBeNull();
}
