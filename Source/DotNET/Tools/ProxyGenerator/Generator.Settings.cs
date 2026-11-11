// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

public static partial class Generator
{
    /// <summary>
    /// Represents the options of a single generation run.
    /// </summary>
    /// <param name="FileHeaderTemplate">Template for the file header.</param>
    /// <param name="OutputPath">The output path to output to.</param>
    /// <param name="SegmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="Message">Logger to use for outputting messages.</param>
    /// <param name="ErrorMessage">Logger to use for outputting error messages.</param>
    /// <param name="LibraryMode">Whether to generate proxies for every public type.</param>
    /// <param name="SkipOutputDeletion">Whether the output path is kept rather than deleted before generating.</param>
    /// <param name="SkipCommandNameInRoute">Whether the command name is skipped in the route.</param>
    /// <param name="SkipQueryNameInRoute">Whether the query name is skipped in the route.</param>
    /// <param name="ApiPrefix">The API prefix to use in the route.</param>
    /// <param name="SkipIndexGeneration">Whether index.ts generation is skipped.</param>
    /// <param name="UseSourceFileAsOutputFile">Whether types are grouped by their C# source file.</param>
    /// <param name="AssemblyPackageMappings">Optional mapping of assembly names to TypeScript package names.</param>
    /// <param name="ExcludedTypeNames">Fully qualified type names excluded from generation.</param>
    /// <param name="ExcludedNamespacePatterns">Namespace glob patterns excluded from generation.</param>
    /// <param name="NamespaceRoots">Pairs of (namespace, base folder) used as roots.</param>
    /// <param name="TypeMappings">Triples of (type name, TypeScript type, package) declaring how a type crosses the wire.</param>
    /// <param name="EmitInterfaces">Whether types are emitted as interfaces rather than classes.</param>
    /// <param name="UseProxyFileSuffix">Whether generated files are named <c>Name.proxy.ts</c>.</param>
    /// <param name="ProducedFiles">Optional collection receiving the produced file paths.</param>
    /// <param name="EndpointGroup">Optional endpoint group.</param>
    /// <param name="Framework">The framework to generate for.</param>
    sealed record GenerationSettings(
        string FileHeaderTemplate,
        string OutputPath,
        int SegmentsToSkip,
        Action<string> Message,
        Action<string> ErrorMessage,
        bool LibraryMode,
        bool SkipOutputDeletion,
        bool SkipCommandNameInRoute,
        bool SkipQueryNameInRoute,
        string ApiPrefix,
        bool SkipIndexGeneration,
        bool UseSourceFileAsOutputFile,
        IReadOnlyDictionary<string, string>? AssemblyPackageMappings,
        IReadOnlyCollection<string>? ExcludedTypeNames,
        IReadOnlyCollection<string>? ExcludedNamespacePatterns,
        IReadOnlyCollection<(string Namespace, string Folder)>? NamespaceRoots,
        IReadOnlyCollection<(string TypeName, string TsType, string Package)>? TypeMappings,
        bool EmitInterfaces,
        bool UseProxyFileSuffix,
        ICollection<string>? ProducedFiles,
        string? EndpointGroup,
        ProxyFramework Framework);
}
