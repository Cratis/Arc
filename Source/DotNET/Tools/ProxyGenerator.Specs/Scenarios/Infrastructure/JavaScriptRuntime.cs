// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System.IO;
using System;

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
        Engine.AddHostObject("__log", new Action<string>(Log));
        InitializeRuntime();
    }

    void Log(string message)
    {
        try
        {
            File.AppendAllText("/tmp/websocket-debug.txt", message + "\n");
        }
        catch { }
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

        if (typeScriptCode.Contains("class ObservableControllerQueryItem") || typeScriptCode.Contains("class ObserveByCategory"))
        {
            try
            {
                File.AppendAllText("/tmp/websocket-debug.txt", $"\n[TranspileTypeScript] {typeScriptCode.Substring(0, Math.Min(50, typeScriptCode.Length))}... transpiled:\n{js}\n");
            }
            catch {}
        }

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
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(
                    "/tmp/websocket-debug.txt",
                    $"\n[JavaScriptRuntime.Execute] Exception: {ex}\nCode:\n{javaScriptCode}\n");
            }
            catch { }

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
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(
                    "/tmp/websocket-debug.txt",
                    $"\n[JavaScriptRuntime.Evaluate<T>] Exception: {ex}\nCode:\n{javaScriptCode}\n");
            }
            catch { }

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
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(
                    "/tmp/websocket-debug.txt",
                    $"\n[JavaScriptRuntime.Evaluate] Exception: {ex}\nCode:\n{javaScriptCode}\n");
            }
            catch { }

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

        // Define console shim
        Engine.Execute(@"
            if (typeof console === 'undefined') {
                globalThis.console = {
                    log: function(msg) { __log('[Console.log] ' + msg); },
                    error: function(msg) { __log('[Console.error] ' + msg); },
                    warn: function(msg) { __log('[Console.warn] ' + msg); },
                    info: function(msg) { __log('[Console.info] ' + msg); }
                };
            }
        ");

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
            
            var checkCode = "'[InitializeRuntime] Loaded reflect-metadata directly. Reflect type: ' + typeof Reflect + ', Reflect.decorate type: ' + (typeof Reflect !== 'undefined' ? typeof Reflect.decorate : 'undefined')";
            var logMsg = Engine.Evaluate(checkCode) as string;
            File.AppendAllText("/tmp/websocket-debug.txt", $"\n{logMsg}\n");
        }
        catch (Exception ex)
        {
            try {
                File.AppendAllText("/tmp/websocket-debug.txt", $"\n[InitializeRuntime] Failed to load reflect-metadata: {ex.Message}\n");
            } catch {}
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
        
        // Debug logging
        var logPath = "/tmp/websocket-debug.txt";
        File.AppendAllText(logPath, $"\n[ReadJavaScriptFile] relativePath={relativePath}\n");
        File.AppendAllText(logPath, $"[ReadJavaScriptFile] baseDir={baseDir}\n");
        File.AppendAllText(logPath, $"[ReadJavaScriptFile] fullPath={fullPath}\n");
        File.AppendAllText(logPath, $"[ReadJavaScriptFile] exists={File.Exists(fullPath)}\n");
        
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
