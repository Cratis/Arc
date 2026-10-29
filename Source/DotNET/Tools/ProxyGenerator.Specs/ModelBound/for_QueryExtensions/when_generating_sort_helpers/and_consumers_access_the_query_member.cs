// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.Sorting;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_generating_sort_helpers;

public class and_consumers_access_the_query_member : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string Compiler = """
        const ts = require(process.argv[1]);
        const path = require('node:path');
        const files = JSON.parse(require('node:fs').readFileSync(0, 'utf8'));
        const options = {
            strict: true, noEmit: true, skipLibCheck: true, types: [],
            target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext,
            moduleResolution: ts.ModuleResolutionKind.Bundler
        };
        const host = ts.createCompilerHost(options);
        const readFile = host.readFile.bind(host);
        const fileExists = host.fileExists.bind(host);
        const directoryExists = host.directoryExists.bind(host);
        host.readFile = file => files[path.resolve(file)] ?? readFile(file);
        host.fileExists = file => Object.hasOwn(files, path.resolve(file)) || fileExists(file);
        host.directoryExists = directory => Object.keys(files).some(file => file.startsWith(path.resolve(directory) + path.sep)) || directoryExists(directory);
        const diagnostics = ts.getPreEmitDiagnostics(ts.createProgram(Object.keys(files), options, host));
        for (const diagnostic of diagnostics) console.error(ts.flattenDiagnosticMessageText(diagnostic.messageText, ' '));
        process.exitCode = diagnostics.length ? 1 : 0;
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _compilerOutput;
    int _compilerExitCode;

    async Task Because()
    {
        var files = new Dictionary<string, string>();
        AddModules(files, typeof(Listing), "ordinary", false);
        AddModules(files, typeof(QueryNamedListing), "collision", true);
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = JavaScriptResources.RepoRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-e");
        start.ArgumentList.Add(Compiler);
        start.ArgumentList.Add(JavaScriptResources.TypeScriptCompilerPath);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(files));
        process.StandardInput.Close();
        await process.WaitForExitAsync();
        _compilerExitCode = process.ExitCode;
        _compilerOutput = await output + await error;
    }

    [Fact] void should_compile_public_query_member_access_and_colliding_accessors() => _compilerExitCode.ShouldEqual(0);
    [Fact] void should_not_report_type_errors() => _compilerOutput.ShouldBeEmpty();

    static void AddModules(Dictionary<string, string> files, Type type, string folder, bool hasQueryProperty)
    {
        var readModel = type.GetTypeInfo();
        var outputPath = Path.Combine(JavaScriptResources.RepoRoot, ".ai-work", "sort-compat", folder);
        var descriptors = readModel.ToQueryDescriptors(outputPath, readModel.Namespace!.Split('.').Length, false, "api", [readModel]);
        files[Path.Combine(outputPath, GeneratedFileNames.FileNameFor(type.Name))] = InMemoryProxyGenerator.GenerateType(type.ToTypeDescriptor(outputPath, 0));
        foreach (var descriptor in descriptors.Where(_ => string.Equals(_.Name, nameof(Listing.AllAsArray), StringComparison.Ordinal) || string.Equals(_.Name, nameof(Listing.ObserveAll), StringComparison.Ordinal)))
        {
            files[Path.Combine(outputPath, GeneratedFileNames.FileNameFor(descriptor.Name))] = InMemoryProxyGenerator.GenerateQuery(descriptor);
        }

        var access = hasQueryProperty ? "q.sortBy.query.ascending();" : "const owner: AllAsArray = q.sortBy.query; const same: boolean = q.sortBy.query === q;";
        var observableAccess = hasQueryProperty ? "o.sortBy.query.ascending();" : "const observableOwner: ObserveAll = o.sortBy.query; const sameObservable: boolean = o.sortBy.query === o;";
        files[Path.Combine(outputPath, "usage.ts")] =
            $"import {{ AllAsArray }} from './{GeneratedFileNames.ModuleNameFor(nameof(Listing.AllAsArray))}';\n" +
            $"import {{ ObserveAll }} from './{GeneratedFileNames.ModuleNameFor(nameof(Listing.ObserveAll))}';\n" +
            $"const q = new AllAsArray(); {access}\nconst o = new ObserveAll(); {observableAccess}\n" +
            (hasQueryProperty ? "AllAsArray.sortBy.query.ascending; ObserveAll.sortBy.query.ascending;" : "AllAsArray.sortBy.name.ascending; ObserveAll.sortBy.name.ascending;");
    }
}
