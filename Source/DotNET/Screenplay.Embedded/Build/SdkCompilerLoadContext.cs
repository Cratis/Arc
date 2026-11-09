// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Keeps generation and its generators on one compiler while sharing the MSBuild task contracts with the host.
/// </summary>
/// <param name="taskAssembly">The task assembly in the host's load context.</param>
/// <param name="compilerDirectory">The consuming SDK's compiler directory.</param>
public sealed class SdkCompilerLoadContext(Assembly taskAssembly, string compilerDirectory)
    : AssemblyLoadContext("Cratis.Arc.Screenplay.Embedded.SdkCompiler", isCollectible: true)
{
    readonly string _taskDirectory = Path.GetDirectoryName(taskAssembly.Location)!;
    readonly AssemblyLoadContext _host = GetLoadContext(taskAssembly)!;

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not { } name)
        {
            return null;
        }

        if (name.StartsWith("Microsoft.Build.", StringComparison.Ordinal) || name.Equals("Microsoft.NET.StringTools", StringComparison.Ordinal))
        {
            return _host.LoadFromAssemblyName(assemblyName);
        }

        if (name.Equals("Microsoft.CodeAnalysis", StringComparison.Ordinal) || name.Equals("Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal))
        {
            return LoadFromAssemblyPath(Path.Combine(compilerDirectory, $"{name}.dll"));
        }

        // Platform assemblies must come from the running SDK's runtime, not older package copies beside the task.
        if (IsPlatformAssembly(name))
        {
            return null;
        }

        var path = Path.Combine(_taskDirectory, $"{name}.dll");

        return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
    }

    static bool IsPlatformAssembly(string name) =>
        name.Equals("System", StringComparison.Ordinal) ||
        name.Equals("netstandard", StringComparison.Ordinal) ||
        name.Equals("mscorlib", StringComparison.Ordinal) ||
        name.StartsWith("System.", StringComparison.Ordinal);
}
