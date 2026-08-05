// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator;
using static Cratis.Arc.ProxyGenerator.PathHelpers;

Console.WriteLine("Cratis Proxy Generator\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: ");
    Console.WriteLine("  Cratis.ProxyGenerator <assembly> <output-path> [segments-to-skip] [--library-mode] [--skip-output-deletion] [--skip-command-name-in-route] [--skip-query-name-in-route] [--api-prefix=<prefix>] [--skip-index-generation] [--use-source-file-as-output-file] [--assembly-to-package=<Assembly>=<Package>]... [--exclude-type=<FullyQualifiedTypeName>]... [--exclude-namespace=<Pattern>]... [--namespace-root=<Namespace>=<Folder>]... [--type-to-ts=<FullyQualifiedTypeName>=<TsType>[=<Package>]]...");
    return 1;
}
var assemblyFile = Normalize(Path.GetFullPath(args[0]));
var outputPath = Normalize(Path.GetFullPath(args[1]));
var segmentsToSkip = args.Length > 2 && !args[2].StartsWith("--") && int.TryParse(args[2], out var segments) ? segments : 0;
var libraryMode = args.Any(_ => _ == "--library-mode");
var skipOutputDeletion = args.Any(_ => _ == "--skip-output-deletion");
var skipCommandNameInRoute = args.Any(_ => _ == "--skip-command-name-in-route");
var skipQueryNameInRoute = args.Any(_ => _ == "--skip-query-name-in-route");
var apiPrefixArg = args.FirstOrDefault(_ => _.StartsWith("--api-prefix="));
var apiPrefix = apiPrefixArg is null ? "api" : apiPrefixArg.Split('=')[^1];
var skipIndexGeneration = args.Any(_ => _ == "--skip-index-generation");
var useSourceFileAsOutputFile = args.Any(_ => _ == "--use-source-file-as-output-file");

var assemblyPackageMappings = new Dictionary<string, string>();
foreach (var mapping in args.Where(_ => _.StartsWith("--assembly-to-package=")).Select(_ => _["--assembly-to-package=".Length..]))
{
    var separatorIndex = mapping.IndexOf('=');
    if (separatorIndex > 0)
    {
        assemblyPackageMappings[mapping[..separatorIndex]] = mapping[(separatorIndex + 1)..];
    }
}

var excludedTypeNames = args
    .Where(_ => _.StartsWith("--exclude-type="))
    .Select(_ => _["--exclude-type=".Length..])
    .ToList();

var excludedNamespacePatterns = args
    .Where(_ => _.StartsWith("--exclude-namespace="))
    .Select(_ => _["--exclude-namespace=".Length..])
    .ToList();

// A repeatable mapping of a .NET type to the TypeScript type it should cross the wire as, fed from a
// TypeToTsType MSBuild item. Consulted ahead of the generator's built-in map, so it can correct an
// existing mapping as well as declare one the generator has never seen.
var typeMappings = new List<(string TypeName, string TsType, string Package)>();
foreach (var entry in args.Where(_ => _.StartsWith("--type-to-ts=")).Select(_ => _["--type-to-ts=".Length..]))
{
    // Bounded to three parts so an '=' inside the TypeScript type stays part of the type rather than being
    // read as the package separator, the way --assembly-to-package and --namespace-root already slice to the
    // end. An unbounded Split dropped everything past the third field without saying so.
    var parts = entry.Split('=', 3);
    if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
    {
        typeMappings.Add((parts[0], parts[1], parts.Length > 2 ? parts[2] : string.Empty));
        continue;
    }

    // Named rather than dropped. A mapping that does not apply produces the generator's built-in type instead,
    // which is a plausible-looking result for a declaration that never took effect - the failure is only
    // visible in generated output nobody reads until it is wrong.
    //
    // On stdout, not stderr: the build invokes this through Exec, which treats what arrives on stderr as build
    // errors, so an ignorable entry would fail the build outright. Exec sets ConsoleToMsBuild, so stdout is
    // carried into the build log either way.
    Console.WriteLine(
        $"warning: ignoring unusable --type-to-ts entry '{entry}'. Expected <FullyQualifiedTypeName>=<TsType>[=<Package>] with both a type name and a TypeScript type.");
}

var namespaceRoots = new List<(string Namespace, string Folder)>();
foreach (var entry in args.Where(_ => _.StartsWith("--namespace-root=")).Select(_ => _["--namespace-root=".Length..]))
{
    var separatorIndex = entry.IndexOf('=');
    if (separatorIndex > 0)
    {
        namespaceRoots.Add((entry[..separatorIndex], entry[(separatorIndex + 1)..]));
    }
}

Console.WriteLine("\nParameters:");
Console.WriteLine($"Assembly: '{assemblyFile}'");
Console.WriteLine($"Output path: '{outputPath}'");
Console.WriteLine($"Segments to skip: {segmentsToSkip}");
Console.WriteLine($"Library mode: {libraryMode}");
Console.WriteLine($"Skip output deletion: {skipOutputDeletion}");
Console.WriteLine($"Skip command name in route: {skipCommandNameInRoute}");
Console.WriteLine($"Skip query name in route: {skipQueryNameInRoute}");
Console.WriteLine($"API prefix: {apiPrefix}");
Console.WriteLine($"Skip index generation: {skipIndexGeneration}");
Console.WriteLine($"Use source file as output file: {useSourceFileAsOutputFile}");
if (assemblyPackageMappings.Count > 0)
{
    Console.WriteLine("Assembly-to-package mappings:");
    foreach (var (assembly, package) in assemblyPackageMappings)
    {
        Console.WriteLine($"  {assembly} -> {package}");
    }
}
if (excludedTypeNames.Count > 0)
{
    Console.WriteLine("Excluded types:");
    foreach (var typeName in excludedTypeNames)
    {
        Console.WriteLine($"  {typeName}");
    }
}
if (excludedNamespacePatterns.Count > 0)
{
    Console.WriteLine("Excluded namespace patterns:");
    foreach (var pattern in excludedNamespacePatterns)
    {
        Console.WriteLine($"  {pattern}");
    }
}
if (namespaceRoots.Count > 0)
{
    Console.WriteLine("Namespace roots:");
    foreach (var (ns, folder) in namespaceRoots)
    {
        Console.WriteLine($"  {ns} -> {folder}");
    }
}
Console.WriteLine();

var result = await Generator.Generate(
    assemblyFile,
    outputPath,
    segmentsToSkip,
    Console.WriteLine,
    Console.Error.WriteLine,
    libraryMode,
    skipOutputDeletion,
    skipCommandNameInRoute,
    skipQueryNameInRoute,
    apiPrefix,
    skipIndexGeneration,
    useSourceFileAsOutputFile,
    assemblyPackageMappings,
    excludedTypeNames,
    excludedNamespacePatterns,
    namespaceRoots,
    typeMappings);
return result ? 0 : 1;