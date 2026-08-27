// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
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
        // The repository root - and with it the yarn workspace's single hoisted node_modules and the
        // Source/JavaScript tree - is resolved once, deterministically, from the global.json marker in
        // JavaScriptResources. Walking the assembly's own directory hierarchy for the nearest ancestor named
        // "node_modules"/"JavaScript" is not deterministic: a build target may copy a partial node_modules
        // folder into one target framework's own bin output, and that nearer, incomplete copy would then shadow
        // the real workspace root for that framework only.
        _workspaceRoot = JavaScriptResources.NodeModulesRoot;
        _javaScriptDirectory = Path.Join(JavaScriptResources.RepoRoot, "Source", "JavaScript");

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
    /// <param name="experimentalDecorators">Whether the legacy TypeScript decorator transform is enabled.</param>
    /// <returns>The transpiled JavaScript code.</returns>
    public string TranspileTypeScript(string typeScriptCode, bool experimentalDecorators = true)
    {
        var escapedCode = EscapeForTemplateLiteral(typeScriptCode);
        var decoratorOptions = experimentalDecorators
            ? "experimentalDecorators: true, emitDecoratorMetadata: true"
            : "experimentalDecorators: false";
        var result = Evaluate($"ts.transpile(`{escapedCode}`, {{ target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS, {decoratorOptions} }})");
        return result?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Gets the syntactic diagnostics the TypeScript compiler reports for a piece of code.
    /// </summary>
    /// <param name="typeScriptCode">The TypeScript code to check.</param>
    /// <param name="experimentalDecorators">Whether the legacy TypeScript decorator transform is enabled.</param>
    /// <returns>The diagnostic messages; empty when the code parses cleanly.</returns>
    /// <remarks>
    /// <see cref="TranspileTypeScript"/> emits best-effort output even for code that does not parse, so a non-empty
    /// transpilation proves nothing. This surfaces what the compiler actually objects to, so a spec can assert on an
    /// empty collection and show the offending messages when it fails.
    /// </remarks>
    public IReadOnlyList<string> GetSyntacticDiagnostics(string typeScriptCode, bool experimentalDecorators = true)
    {
        var escapedCode = EscapeForTemplateLiteral(typeScriptCode);
        var decoratorOptions = experimentalDecorators
            ? "experimentalDecorators: true, emitDecoratorMetadata: true"
            : "experimentalDecorators: false";
        var result = Evaluate($"JSON.stringify((ts.transpileModule(`{escapedCode}`, {{ compilerOptions: {{ target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS, {decoratorOptions} }}, reportDiagnostics: true }}).diagnostics || []).map(diagnostic => ts.flattenDiagnosticMessageText(diagnostic.messageText, ' ')))");
        return JsonSerializer.Deserialize<string[]>(result?.ToString() ?? "[]") ?? [];
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
        // Use cached TypeScript compiler and bootstrap code to avoid disk I/O on every test
        // The SharedJavaScriptRuntimeFixture loads these once on first access
        Engine.Execute(SharedJavaScriptRuntimeFixture.TypeScriptCompilerCode);
        Engine.Execute(SharedJavaScriptRuntimeFixture.ArcBootstrapCode);

        // Ensure a global module/exports shim exists so scripts executed directly
        // (outside the module loader) that reference `exports`/`module` do not
        // throw ReferenceError: exports is not defined.
        Engine.Execute("\n" +
                       "            if (!globalThis.module) { globalThis.module = { exports: {} }; }\n" +
                       "            if (!globalThis.exports) { globalThis.exports = globalThis.module.exports; }\n" +
                       "        ");

        // Load the Reflect metadata polyfill directly so the API is available globally
        // before any script that relies on it runs.
        try
        {
            var reflectionPolyfill = ReadJavaScriptFile("node_modules/@cratis/fundamentals/dist/cjs/reflection.js");
            Engine.Execute(reflectionPolyfill);
        }
        catch
        {
            // Ignore errors loading the reflection polyfill
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

    static string EscapeForTemplateLiteral(string code) =>
        code.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
}
