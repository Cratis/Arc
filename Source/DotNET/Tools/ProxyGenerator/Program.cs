// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator;
using static Cratis.Arc.ProxyGenerator.PathHelpers;

Console.WriteLine("Cratis Proxy Generator\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: ");
    Console.WriteLine("  Cratis.ProxyGenerator <assembly> <output-path> [segments-to-skip] [--skip-output-deletion] [--skip-command-name-in-route] [--skip-query-name-in-route] [--api-prefix=<prefix>] [--project-directory=<path>] [--skip-file-index-tracking]");
    return 1;
}
var assemblyFile = Normalize(Path.GetFullPath(args[0]));
var outputPath = Normalize(Path.GetFullPath(args[1]));
var segmentsToSkip = args.Length > 2 && !args[2].StartsWith("--") ? int.Parse(args[2]) : 0;
var skipOutputDeletion = args.Any(_ => _ == "--skip-output-deletion");
var skipCommandNameInRoute = args.Any(_ => _ == "--skip-command-name-in-route");
var skipQueryNameInRoute = args.Any(_ => _ == "--skip-query-name-in-route");
var apiPrefixArg = args.FirstOrDefault(_ => _.StartsWith("--api-prefix="));
var apiPrefix = apiPrefixArg is null ? "api" : apiPrefixArg.Split('=')[^1];
var projectDirectoryArg = args.FirstOrDefault(_ => _.StartsWith("--project-directory="));
var projectDirectory = projectDirectoryArg is null ? null : Normalize(Path.GetFullPath(projectDirectoryArg.Split('=')[^1]));
var skipFileIndexTracking = args.Any(_ => _ == "--skip-file-index-tracking");

Console.WriteLine("\nParameters:");
Console.WriteLine($"Assembly: '{assemblyFile}'");
Console.WriteLine($"Output path: '{outputPath}'");
Console.WriteLine($"Segments to skip: {segmentsToSkip}");
Console.WriteLine($"Skip output deletion: {skipOutputDeletion}");
Console.WriteLine($"Skip command name in route: {skipCommandNameInRoute}");
Console.WriteLine($"Skip query name in route: {skipQueryNameInRoute}");
Console.WriteLine($"API prefix: {apiPrefix}");
Console.WriteLine($"Project directory: {projectDirectory ?? "(auto-detect from assembly)"}");
Console.WriteLine($"Skip file index tracking: {skipFileIndexTracking}");
Console.WriteLine();

var result = await Generator.Generate(
    assemblyFile,
    outputPath,
    segmentsToSkip,
    Console.WriteLine,
    Console.Error.WriteLine,
    skipOutputDeletion,
    skipCommandNameInRoute,
    skipQueryNameInRoute,
    apiPrefix,
    projectDirectory,
    skipFileIndexTracking);
return result ? 0 : 1;