// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Represents the actual generator.
/// </summary>
public static partial class Generator
{
    /// <summary>
    /// Generate the proxies from an input assembly and output path.
    /// </summary>
    /// <param name="assemblyFile">Path to the assembly file.</param>
    /// <param name="outputPath">The output path to output to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="errorMessage">Logger to use for outputting error messages.</param>
    /// <param name="libraryMode">When <see langword="true"/>, generates TypeScript proxies for every public type in the assembly, not just commands and queries.</param>
    /// <param name="skipOutputDeletion">True if the output path should be deleted before generating, false if not.</param>
    /// <param name="skipCommandNameInRoute">True if the command name should be skipped in the route, false if not.</param>
    /// <param name="skipQueryNameInRoute">True if the query name should be skipped in the route, false if not.</param>
    /// <param name="apiPrefix">The API prefix to use in the route.</param>
    /// <param name="skipIndexGeneration">True to skip index.ts file generation, false to enable it.</param>
    /// <param name="useSourceFileAsOutputFile">True to group all TypeScript types from the same C# source file into a single .ts file named after the source file, false to generate one file per type.</param>
    /// <param name="assemblyPackageMappings">Optional dictionary mapping assembly names to TypeScript package names. Types from these assemblies will be imported from the package instead of being generated locally.</param>
    /// <param name="excludedTypeNames">Fully qualified type names that should be excluded from proxy generation.</param>
    /// <param name="excludedNamespacePatterns">Namespace glob patterns (supports <c>*</c> as wildcard) whose matching types are excluded from generation.</param>
    /// <param name="namespaceRoots">Pairs of (namespace, base folder) used as roots. When a type's namespace begins with a root the root is stripped and the remainder is placed under the base folder.</param>
    /// <param name="typeMappings">Triples of (fully qualified type name, TypeScript type, package) declaring how a type crosses the wire. Consulted ahead of the built-in map, so it can correct an existing mapping as well as add an unknown one.</param>
    /// <param name="emitInterfaces">Emit types as plain interfaces rather than classes, with no decorators and no runtime dependency on <c>@cratis/fundamentals</c>.</param>
    /// <param name="useProxyFileSuffix">Name generated files <c>Name.proxy.ts</c> rather than <c>Name.ts</c>, to tell them apart from hand-written TypeScript.</param>
    /// <returns>True if successful, false if not.</returns>
    public static Task<bool> Generate(
        string assemblyFile,
        string outputPath,
        int segmentsToSkip,
        Action<string> message,
        Action<string> errorMessage,
        bool libraryMode = false,
        bool skipOutputDeletion = true,
        bool skipCommandNameInRoute = false,
        bool skipQueryNameInRoute = false,
        string apiPrefix = "api",
        bool skipIndexGeneration = false,
        bool useSourceFileAsOutputFile = false,
        IReadOnlyDictionary<string, string>? assemblyPackageMappings = null,
        IReadOnlyCollection<string>? excludedTypeNames = null,
        IReadOnlyCollection<string>? excludedNamespacePatterns = null,
        IReadOnlyCollection<(string Namespace, string Folder)>? namespaceRoots = null,
        IReadOnlyCollection<(string TypeName, string TsType, string Package)>? typeMappings = null,
        bool emitInterfaces = false,
        bool useProxyFileSuffix = false) => GenerateWithFileHeader(string.Empty, assemblyFile, outputPath, segmentsToSkip, message, errorMessage, libraryMode, skipOutputDeletion, skipCommandNameInRoute, skipQueryNameInRoute, apiPrefix, skipIndexGeneration, useSourceFileAsOutputFile, assemblyPackageMappings, excludedTypeNames, excludedNamespacePatterns, namespaceRoots, typeMappings, emitInterfaces, useProxyFileSuffix);

    /// <inheritdoc cref="Generate"/>
    internal static Task<bool> GenerateWithFileHeader(
        string fileHeaderTemplate,
        string assemblyFile,
        string outputPath,
        int segmentsToSkip,
        Action<string> message,
        Action<string> errorMessage,
        bool libraryMode = false,
        bool skipOutputDeletion = true,
        bool skipCommandNameInRoute = false,
        bool skipQueryNameInRoute = false,
        string apiPrefix = "api",
        bool skipIndexGeneration = false,
        bool useSourceFileAsOutputFile = false,
        IReadOnlyDictionary<string, string>? assemblyPackageMappings = null,
        IReadOnlyCollection<string>? excludedTypeNames = null,
        IReadOnlyCollection<string>? excludedNamespacePatterns = null,
        IReadOnlyCollection<(string Namespace, string Folder)>? namespaceRoots = null,
        IReadOnlyCollection<(string TypeName, string TsType, string Package)>? typeMappings = null,
        bool emitInterfaces = false,
        bool useProxyFileSuffix = false,
        ICollection<string>? producedFiles = null,
        string? endpointGroup = null,
        ProxyFramework framework = ProxyFramework.React) =>
        new GenerationRun(new GenerationSettings(
            fileHeaderTemplate,
            outputPath,
            segmentsToSkip,
            message,
            errorMessage,
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
            typeMappings,
            emitInterfaces,
            useProxyFileSuffix,
            producedFiles,
            endpointGroup,
            framework)).Execute(assemblyFile);
}
