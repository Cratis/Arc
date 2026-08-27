// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Provides file paths for JavaScript resources used in testing.
/// </summary>
public static class JavaScriptResources
{
    static JavaScriptResources()
    {
        RepoRoot = FindRepositoryRoot(AppContext.BaseDirectory)
            ?? throw new DirectoryNotFoundException("Could not find the Arc repository root");
        ScenariosRoot = Path.Join(RepoRoot, "Source", "DotNET", "Tools", "ProxyGenerator.Specs", "Scenarios");
        NodeModulesRoot = Directory.Exists(Path.Join(RepoRoot, "node_modules"))
            ? RepoRoot
            : AppContext.BaseDirectory;
    }

    /// <summary>
    /// Gets the path to the Scenarios folder.
    /// </summary>
    public static string ScenariosRoot { get; }

    /// <summary>
    /// Gets the path to the repository root.
    /// </summary>
    public static string RepoRoot { get; }

    /// <summary>
    /// Gets the path to the directory that contains the JavaScript runtime dependencies.
    /// </summary>
    public static string NodeModulesRoot { get; }

    /// <summary>
    /// Gets the path to the TypeScript compiler.
    /// </summary>
    /// <remarks>
    /// TypeScript 7 ships a native compiler that no longer exposes lib/typescript.js, so the classic
    /// 'typescript-for-eslint' alias (npm:typescript@6.0.3) is preferred when present, falling back to the
    /// root 'typescript' package for environments that still resolve a classic TypeScript.
    /// </remarks>
    public static string TypeScriptCompilerPath
    {
        get
        {
            var forEslintPath = Path.Join(NodeModulesRoot, "node_modules", "typescript-for-eslint", "lib", "typescript.js");
            return File.Exists(forEslintPath)
                ? forEslintPath
                : Path.Join(NodeModulesRoot, "node_modules", "typescript", "lib", "typescript.js");
        }
    }

    /// <summary>
    /// Gets the path to the Arc package CJS directory.
    /// </summary>
    public static string ArcPackagePath =>
        Path.Join(RepoRoot, "Source", "JavaScript", "Arc", "dist", "cjs");

    /// <summary>
    /// Gets the path to the Arc.React package CJS directory.
    /// </summary>
    public static string ArcReactPackagePath =>
        Path.Join(RepoRoot, "Source", "JavaScript", "Arc.React", "dist", "cjs");

    /// <summary>
    /// Gets the path to the Fundamentals package CJS directory.
    /// </summary>
    public static string FundamentalsPackagePath =>
        Path.Join(NodeModulesRoot, "node_modules", "@cratis", "fundamentals", "dist", "cjs");

    /// <summary>
    /// Reads the TypeScript compiler source.
    /// </summary>
    /// <returns>The TypeScript compiler JavaScript code.</returns>
    /// <exception cref="TypeScriptCompilerNotFound">The exception that is thrown when the TypeScript compiler is not found.</exception>
    public static string GetTypeScriptCompiler()
    {
        var path = TypeScriptCompilerPath;
        if (!File.Exists(path))
        {
            throw new TypeScriptCompilerNotFound(path);
        }

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Gets the Arc runtime bootstrap code that sets up the module environment.
    /// </summary>
    /// <returns>JavaScript code to bootstrap Arc modules.</returns>
    public static string GetArcBootstrap() => EmbeddedResources.GetArcBootstrap();

    static string? FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            var hasRootFiles = File.Exists(Path.Join(current.FullName, "global.json")) &&
                               File.Exists(Path.Join(current.FullName, "package.json"));
            var hasProxySpecs = Directory.Exists(Path.Join(current.FullName, "Source", "DotNET", "Tools", "ProxyGenerator.Specs"));
            if (hasRootFiles && hasProxySpecs)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
