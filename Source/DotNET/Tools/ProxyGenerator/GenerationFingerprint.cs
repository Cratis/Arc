// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyModel;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Computes the content fingerprint of proxy generation inputs.
/// </summary>
internal static class GenerationFingerprint
{
    /// <summary>
    /// Computes a fingerprint without loading the input assemblies.
    /// </summary>
    /// <param name="assemblyFile">The target assembly file.</param>
    /// <param name="arguments">The ordered generation arguments, excluding the state file.</param>
    /// <param name="fileHeaderTemplate">The resolved generated file header.</param>
    /// <param name="toolIdentity">An optional override for the generator identity.</param>
    /// <returns>The fingerprint, or null when the inputs cannot be read.</returns>
    internal static string? Compute(string assemblyFile, IEnumerable<string> arguments, string fileHeaderTemplate, string? toolIdentity = null)
    {
        var depsFile = Path.ChangeExtension(assemblyFile, ".deps.json");
        if (!File.Exists(assemblyFile) || !File.Exists(depsFile)) return null;

        try
        {
            using var depsStream = File.OpenRead(depsFile);
            using var reader = new DependencyContextJsonReader();
            var context = reader.Read(depsStream);
            if (context is null) return null;

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Append(hash, "cratis-proxies-state-v2");
            Append(hash, toolIdentity ?? GetToolIdentity());
            Append(hash, RuntimeEnvironment.GetRuntimeDirectory());
            Append(hash, Environment.CurrentDirectory);
            foreach (var argument in arguments) Append(hash, argument);
            Append(hash, fileHeaderTemplate);
            AppendFile(hash, depsFile);
            AppendManagedAssemblies(hash, context, Path.GetFullPath(assemblyFile));
            return Convert.ToHexString(hash.GetHashAndReset());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            // An unavailable or unreadable input must never produce a cache hit.
            return null;
        }
    }

    static void AppendManagedAssemblies(IncrementalHash hash, DependencyContext context, string assemblyFile)
    {
        var paths = TypeExtensions.GetRequiredAssemblyPaths(context, Path.GetDirectoryName(assemblyFile)!, assemblyFile);
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal))
        {
            AppendFile(hash, path);
            AppendFile(hash, Path.ChangeExtension(path, ".pdb"));
            AppendFile(hash, Path.ChangeExtension(path, ".xml"));
        }
    }

    static void AppendFile(IncrementalHash hash, string path)
    {
        Append(hash, path);
        if (!File.Exists(path))
        {
            Append(hash, "missing");
            return;
        }

        using var stream = File.OpenRead(path);
        Append(hash, Convert.ToHexString(SHA256.HashData(stream)));
    }

    static void Append(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        hash.AppendData(BitConverter.GetBytes(bytes.Length));
        hash.AppendData(bytes);
    }

    static string GetToolIdentity()
    {
        var assembly = typeof(Generator).Assembly;
        return $"{assembly.ManifestModule.ModuleVersionId}:{assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}";
    }
}
