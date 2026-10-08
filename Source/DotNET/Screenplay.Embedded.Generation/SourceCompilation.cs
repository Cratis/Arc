// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Builds the compilation the documents of a project are generated from.
/// </summary>
/// <remarks>
/// The build already knows what the C# compiler is about to be given - the files, the references, the symbols,
/// the language version and the effective features - so the same inputs are parsed here rather than a workspace being loaded or a project file
/// being read a second time. A compilation built from anything else would describe a different application than the
/// one being built.
/// </remarks>
public static class SourceCompilation
{
    /// <summary>
    /// Creates the compilation.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly being built.</param>
    /// <param name="sources">The full path of every file being compiled.</param>
    /// <param name="references">The full path of every assembly being referenced.</param>
    /// <param name="defineConstants">The preprocessor symbols the source is compiled with, separated by <c>;</c>.</param>
    /// <param name="languageVersion">The language version the source is compiled with.</param>
    /// <param name="outputType">The project output type, defaulting to a library.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation Create(
        string assemblyName,
        IEnumerable<string> sources,
        IEnumerable<string> references,
        string? defineConstants,
        string? languageVersion,
        string? outputType = null) =>
        Create(assemblyName, sources, references, defineConstants, languageVersion, outputType, null, null, null);

    /// <summary>
    /// Creates the compilation with the effective compiler features supplied by MSBuild.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly being built.</param>
    /// <param name="sources">The full path of every file being compiled.</param>
    /// <param name="references">The full path of every assembly being referenced.</param>
    /// <param name="defineConstants">The preprocessor symbols separated by <c>;</c>.</param>
    /// <param name="languageVersion">The language version the source is compiled with.</param>
    /// <param name="outputType">The project output type.</param>
    /// <param name="features">The MSBuild compiler features separated by semicolons, commas or spaces.</param>
    /// <param name="interceptorsNamespaces">The namespaces allowed to declare interceptors, separated by semicolons.</param>
    /// <param name="interceptorsPreviewNamespaces">The legacy alias for interceptor namespaces, added to the current namespaces.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation Create(
        string assemblyName,
        IEnumerable<string> sources,
        IEnumerable<string> references,
        string? defineConstants,
        string? languageVersion,
        string? outputType,
        string? features,
        string? interceptorsNamespaces,
        string? interceptorsPreviewNamespaces)
    {
        var parseOptions = new CSharpParseOptions(
            LanguageVersionOf(languageVersion),
            preprocessorSymbols: SymbolsOf(defineConstants))
            .WithFeatures(CompilerFeatures.Parse(features, interceptorsNamespaces, interceptorsPreviewNamespaces));

        var trees = sources
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => CSharpSyntaxTree.ParseText(Read(_), parseOptions, _))
            .ToList();

        var metadata = references
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => (MetadataReference)MetadataReference.CreateFromFile(_))
            .ToList();

        return CSharpCompilation.Create(
            assemblyName,
            trees,
            metadata,
            new CSharpCompilationOptions(OutputKindOf(outputType), allowUnsafe: true));
    }

    /// <summary>
    /// Resolves the compiler output kind, including top-level statement applications.
    /// </summary>
    /// <param name="outputType">The MSBuild output type.</param>
    /// <returns>The matching compiler output kind.</returns>
    static OutputKind OutputKindOf(string? outputType) => outputType?.ToUpperInvariant() switch
    {
        "EXE" => OutputKind.ConsoleApplication,
        "WINEXE" => OutputKind.WindowsApplication,
        "MODULE" => OutputKind.NetModule,
        _ => OutputKind.DynamicallyLinkedLibrary
    };

    /// <summary>
    /// Reads a source file the way the compiler reads it.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The <see cref="SourceText"/>.</returns>
    static SourceText Read(string path)
    {
        using var stream = File.OpenRead(path);

        return SourceText.From(stream, Encoding.UTF8);
    }

    /// <summary>
    /// Gets the preprocessor symbols the source is compiled with.
    /// </summary>
    /// <param name="defineConstants">The symbols as the build states them.</param>
    /// <returns>The symbols.</returns>
    static IEnumerable<string> SymbolsOf(string? defineConstants) =>
        string.IsNullOrWhiteSpace(defineConstants)
            ? []
            : defineConstants
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(_ => _.Trim())
                .Where(_ => _.Length > 0)
                .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Gets the language version the source is compiled with.
    /// </summary>
    /// <param name="languageVersion">The version as the build states it.</param>
    /// <returns>The <see cref="LanguageVersion"/>, the latest one when the build states nothing usable.</returns>
    static LanguageVersion LanguageVersionOf(string? languageVersion) =>
        !string.IsNullOrWhiteSpace(languageVersion) && LanguageVersionFacts.TryParse(languageVersion, out var parsed)
            ? parsed
            : LanguageVersion.Latest;
}
