// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ClearScript.V8;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Represents a JavaScript runtime environment using V8 engine with TypeScript transpilation support.
/// </summary>
public sealed class JavaScriptRuntime : IDisposable
{
    readonly string _javaScriptDirectory;
    readonly string _workspaceRoot;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptRuntime"/> class.
    /// </summary>
    public JavaScriptRuntime()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(JavaScriptRuntime).Assembly.Location)!;

        // Find workspace root by looking for node_modules directory
        _workspaceRoot = FindDirectoryInHierarchy(assemblyDir, "node_modules")
            ?? throw new DirectoryNotFoundException("Could not find workspace root (node_modules directory not found in parent hierarchy)");

        // Find JavaScript source directory
        _javaScriptDirectory = FindDirectoryInHierarchy(assemblyDir, "JavaScript")
            ?? throw new DirectoryNotFoundException("Could not find JavaScript source directory in parent hierarchy");

        Engine = new V8ScriptEngine();
        Engine.AddHostObject("__readTypeScriptFile", new Func<string, string>(ReadTypeScriptFile));
        Engine.AddHostObject("__readJavaScriptFile", new Func<string, string>(ReadJavaScriptFile));
        Engine.AddHostObject("__fileExists", new Func<string, bool>(FileExists));
        InitializeRuntime();
    }

    /// <summary>
    /// Gets the underlying V8 script engine.
    /// </summary>
    public V8ScriptEngine Engine { get; }

    /// <summary>
    /// Transpiles TypeScript code to JavaScript.
    /// </summary>
    /// <param name="typeScriptCode">The TypeScript code to transpile.</param>
    /// <returns>The transpiled JavaScript code.</returns>
    public string TranspileTypeScript(string typeScriptCode)
    {
        var escapedCode = typeScriptCode.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
        var result = Evaluate($"ts.transpile(`{escapedCode}`, {{ target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS, experimentalDecorators: true, emitDecoratorMetadata: true }})");
        return result?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Executes JavaScript code in the runtime.
    /// </summary>
    /// <param name="javaScriptCode">The JavaScript code to execute.</param>
    public void Execute(string javaScriptCode)
    {
        Engine.Execute(javaScriptCode);
    }

    /// <summary>
    /// Executes JavaScript code and returns the result.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="javaScriptCode">The JavaScript code to execute.</param>
    /// <returns>The result of the execution.</returns>
    public T? Evaluate<T>(string javaScriptCode)
    {
        var result = Engine.Evaluate(javaScriptCode);
        if (result is T typedResult)
        {
            return typedResult;
        }

        return default;
    }

    /// <summary>
    /// Executes JavaScript code and returns the raw result.
    /// </summary>
    /// <param name="javaScriptCode">The JavaScript code to execute.</param>
    /// <returns>The result of the execution.</returns>
    public object? Evaluate(string javaScriptCode)
    {
        return Engine.Evaluate(javaScriptCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            Engine.Dispose();
            _disposed = true;
        }
    }

    void InitializeRuntime()
    {
        // Load TypeScript compiler from node_modules
        var typeScriptCompiler = JavaScriptResources.GetTypeScriptCompiler();
        Engine.Execute(typeScriptCompiler);

        // Load Arc bootstrap code (sets up module environment with shims)
        var arcBootstrap = JavaScriptResources.GetArcBootstrap();
        Engine.Execute(arcBootstrap);

        // Ensure a global module/exports shim exists so scripts executed directly
        // (outside the module loader) that reference `exports`/`module` do not
        // throw ReferenceError: exports is not defined.
        Engine.Execute("\n" +
                       "            if (!globalThis.module) { globalThis.module = { exports: {} }; }\n" +
                       "            if (!globalThis.exports) { globalThis.exports = globalThis.module.exports; }\n" +
                       "        ");

        // Load reflect-metadata polyfill directly
        try
        {
            var reflectMetadata = ReadJavaScriptFile("node_modules/reflect-metadata/Reflect.js");
            Engine.Execute(reflectMetadata);
        }
        catch
        {
            // Ignore errors loading reflect-metadata
        }
    }

    string ReadTypeScriptFile(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_javaScriptDirectory, relativePath));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"TypeScript file not found: {relativePath}", fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    string ReadJavaScriptFile(string relativePath)
    {
        // For node_modules, resolve relative to workspace root; otherwise relative to JavaScript directory
        var baseDir = relativePath.StartsWith("node_modules/") ? _workspaceRoot : _javaScriptDirectory;
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"JavaScript file not found: {relativePath}", fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    bool FileExists(string relativePath)
    {
        // For node_modules, resolve relative to workspace root; otherwise relative to JavaScript directory
        var baseDir = relativePath.StartsWith("node_modules/") ? _workspaceRoot : _javaScriptDirectory;
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
        return File.Exists(fullPath);
    }

    static string? FindDirectoryInHierarchy(string startPath, string directoryName)
    {
        var currentDir = new DirectoryInfo(startPath);

        while (currentDir != null)
        {
            var targetPath = Path.Combine(currentDir.FullName, directoryName);
            if (Directory.Exists(targetPath))
            {
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }
}
