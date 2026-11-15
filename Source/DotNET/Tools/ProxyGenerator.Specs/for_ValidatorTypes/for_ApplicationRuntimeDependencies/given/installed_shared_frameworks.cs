// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_ValidatorTypes.for_ApplicationRuntimeDependencies.given;

public class installed_shared_frameworks : Specification
{
    protected string RuntimeDirectory = null!;
    protected AssemblyName RequestedAssembly = new("Microsoft.AspNetCore.Mvc.Core, Version=11.0.0.0");
    protected string? ResolvedPath;
    string _sharedDirectory = null!;

    void Establish()
    {
        _sharedDirectory = Path.Combine(Path.GetTempPath(), $"arc-shared-frameworks-{Guid.NewGuid():N}");
        RuntimeDirectory = Path.Combine(_sharedDirectory, "Microsoft.NETCore.App", "11.0.1");
        Directory.CreateDirectory(RuntimeDirectory);
    }

    protected string Install(string version)
    {
        var directory = Path.Combine(_sharedDirectory, "Microsoft.AspNetCore.App", version);
        Directory.CreateDirectory(directory);
        var assembly = Path.Combine(directory, $"{RequestedAssembly.Name}.dll");
        File.WriteAllText(assembly, string.Empty);
        return assembly;
    }

    void Destroy() => Directory.Delete(_sharedDirectory, true);
}
