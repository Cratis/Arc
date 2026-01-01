// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ClearScript.V8;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Represents a JavaScript runtime environment using V8 engine with TypeScript transpilation support.
/// </summary>
public sealed class JavaScriptRuntime : IDisposable
{
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptRuntime"/> class.
    /// </summary>
    public JavaScriptRuntime()
    {
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
        var js = result?.ToString() ?? string.Empty;

        return js;
    }

    /// <summary>
    /// Executes JavaScript code in the runtime.
    /// </summary>
    /// <param name="javaScriptCode">The JavaScript code to execute.</param>
    public void Execute(string javaScriptCode)
    {
        try
        {
            Engine.Execute(javaScriptCode);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Executes JavaScript code and returns the result.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="javaScriptCode">The JavaScript code to execute.</param>
    /// <returns>The result of the execution.</returns>
    public T? Evaluate<T>(string javaScriptCode)
    {
        try
        {
            var result = Engine.Evaluate(javaScriptCode);
            if (result is T typedResult)
            {
                return typedResult;
            }

            return default;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Executes JavaScript code and returns the raw result.
    /// </summary>
    /// <param name="javaScriptCode">The JavaScript code to execute.</param>
    /// <returns>The result of the execution.</returns>
    public object? Evaluate(string javaScriptCode)
    {
        try
        {
            return Engine.Evaluate(javaScriptCode);
        }
        catch
        {
            throw;
        }
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
        Engine.Execute(@"
            if (!globalThis.module) { globalThis.module = { exports: {} }; }
            if (!globalThis.exports) { globalThis.exports = globalThis.module.exports; }
        ");

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
        // Resolve paths relative to the Source/JavaScript directory
        var baseDir = Path.Combine(
            Path.GetDirectoryName(typeof(JavaScriptRuntime).Assembly.Location)!,
            "..", "..", "..", "..", "..", "..", "JavaScript");
        
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"TypeScript file not found: {relativePath}", fullPath);
        }
        
        return File.ReadAllText(fullPath);
    }

    string ReadJavaScriptFile(string relativePath)
    {
        // For node_modules, resolve relative to workspace root; otherwise relative to Source/JavaScript
        var baseDir = relativePath.StartsWith("node_modules/")
            ? Path.Combine(
                Path.GetDirectoryName(typeof(JavaScriptRuntime).Assembly.Location)!,
                "..", "..", "..", "..", "..", "..", "..")  // 7 levels up to workspace root
            : Path.Combine(
                Path.GetDirectoryName(typeof(JavaScriptRuntime).Assembly.Location)!,
                "..", "..", "..", "..", "..", "..", "JavaScript");  // 6 levels up to Source, then into JavaScript
        
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"JavaScript file not found: {relativePath}", fullPath);
        }
        
        return File.ReadAllText(fullPath);
    }

    bool FileExists(string relativePath)
    {
        // For node_modules, resolve relative to workspace root; otherwise relative to Source/JavaScript
        var baseDir = relativePath.StartsWith("node_modules/")
            ? Path.Combine(
                Path.GetDirectoryName(typeof(JavaScriptRuntime).Assembly.Location)!,
                "..", "..", "..", "..", "..", "..", "..")  // 7 levels up to workspace root
            : Path.Combine(
                Path.GetDirectoryName(typeof(JavaScriptRuntime).Assembly.Location)!,
                "..", "..", "..", "..", "..", "..", "JavaScript");  // 6 levels up to Source, then into JavaScript
        
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
        return File.Exists(fullPath);
    }
}
