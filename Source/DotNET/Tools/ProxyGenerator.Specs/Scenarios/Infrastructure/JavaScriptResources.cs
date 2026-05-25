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
        // Find the Scenarios folder by looking for package.json
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var packageJson = Path.Combine(current, "package.json");
            if (File.Exists(packageJson) && current.EndsWith("Scenarios", StringComparison.OrdinalIgnoreCase))
            {
                ScenariosRoot = current;
                break;
            }

            current = Path.GetDirectoryName(current);
        }

        // If not found, calculate from assembly location (development scenario)
        if (string.IsNullOrEmpty(ScenariosRoot))
        {
            // From bin/Debug/net10.0 go up to ProxyGenerator.Specs then into Scenarios
            var assemblyDir = Path.GetDirectoryName(typeof(JavaScriptResources).Assembly.Location);
            ScenariosRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "Scenarios"));
        }

        // Find repository root (has global.json)
        current = ScenariosRoot;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "global.json")))
            {
                RepoRoot = current;
                break;
            }

            current = Path.GetDirectoryName(current);
        }

        RepoRoot ??= Path.GetFullPath(Path.Combine(ScenariosRoot, "..", "..", "..", "..", "..", ".."));

        NodeModulesRoot = FindDirectoryContaining(AppContext.BaseDirectory, "node_modules") ?? RepoRoot;
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
    public static string TypeScriptCompilerPath =>
        Path.Combine(NodeModulesRoot, "node_modules", "typescript", "lib", "typescript.js");

    /// <summary>
    /// Gets the path to the Arc package CJS directory.
    /// </summary>
    public static string ArcPackagePath =>
        Path.Combine(RepoRoot, "Source", "JavaScript", "Arc", "dist", "cjs");

    /// <summary>
    /// Gets the path to the Arc.React package CJS directory.
    /// </summary>
    public static string ArcReactPackagePath =>
        Path.Combine(RepoRoot, "Source", "JavaScript", "Arc.React", "dist", "cjs");

    /// <summary>
    /// Gets the path to the Fundamentals package CJS directory.
    /// </summary>
    public static string FundamentalsPackagePath =>
        Path.Combine(NodeModulesRoot, "node_modules", "@cratis", "fundamentals", "dist", "cjs");

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

    static string? FindDirectoryContaining(string startPath, string directoryName)
    {
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, directoryName)))
            {
                return current;
            }

            current = Path.GetDirectoryName(current);
        }

        return null;
    }
}
