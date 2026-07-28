// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Compiles C# source and recovers the model it describes.
/// </summary>
/// <remarks>
/// This is what makes every source analysis specification hermetic - a compilation is built from source strings in
/// memory, so there is no project file, no build output, no workspace and nothing mocked. What is asserted is what
/// a real compilation of that source would yield.
/// </remarks>
public static class Analyzed
{
    /// <summary>
    /// The name given to the assembly every specification compiles into.
    /// </summary>
    public const string AssemblyName = "Library";

    /// <summary>
    /// The path a single source file is compiled as.
    /// </summary>
    public const string SlicePath = "Library/Feature/Slice/Slice.cs";

    /// <summary>
    /// The file every compilation carries so that paths in the document start at the root of the project.
    /// </summary>
    /// <remarks>
    /// Paths are written relative to the deepest directory every source file shares, which for a real project is
    /// its root. A compilation of a single file would make that file's own folder the root, so the entry point
    /// every project has stands in for the rest of it.
    /// </remarks>
    public static readonly (string Path, string Text) Root = ("Library/Program.cs", "namespace Library;");

    static readonly MetadataReference[] _references =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => MetadataReference.CreateFromFile(_))
    ];

    /// <summary>
    /// Compiles source and recovers the model it describes, with nothing sitting alongside it.
    /// </summary>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    public static ApplicationModelAnalysis Source(params (string Path, string Text)[] sources) =>
        Source(DeclaredUserInterfaceFiles.None, sources);

    /// <summary>
    /// Compiles source and recovers the model it describes.
    /// </summary>
    /// <param name="files">The user interface files sitting alongside the source.</param>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    /// <remarks>
    /// The files a screen is recovered from are declared rather than written to a disk, so that a specification
    /// about screens is exactly as hermetic as every other one.
    /// </remarks>
    public static ApplicationModelAnalysis Source(IUserInterfaceFiles files, params (string Path, string Text)[] sources) =>
        new ApplicationModelAnalyzer(files).Analyze(Compile(sources), new ScreenplayOptions().WithDefaults(AssemblyName));

    /// <summary>
    /// Compiles a single source file and recovers the model it describes, with nothing sitting alongside it.
    /// </summary>
    /// <param name="text">The source.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    public static ApplicationModelAnalysis Source(string text) => Source((SlicePath, text));

    /// <summary>
    /// Compiles a single source file and recovers the model it describes.
    /// </summary>
    /// <param name="files">The user interface files sitting alongside the source.</param>
    /// <param name="text">The source.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    public static ApplicationModelAnalysis Source(IUserInterfaceFiles files, string text) => Source(files, (SlicePath, text));

    /// <summary>
    /// Compiles source into a compilation.
    /// </summary>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation Compile(params (string Path, string Text)[] sources) =>
        CSharpCompilation.Create(
            AssemblyName,
            sources.Append(Root).Select(_ => CSharpSyntaxTree.ParseText(
                _.Text,
                new CSharpParseOptions(documentationMode: DocumentationMode.Parse),
                path: _.Path)),
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    /// <summary>
    /// Gets everything the compiler itself reported, so that a specification never asserts against broken source.
    /// </summary>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The errors, empty when the source compiles.</returns>
    public static IEnumerable<string> ErrorsIn(params (string Path, string Text)[] sources) =>
        Compile(sources)
            .GetDiagnostics()
            .Where(_ => _.Severity == DiagnosticSeverity.Error)
            .Select(_ => _.ToString());

    /// <summary>
    /// Gets the single slice a model describes.
    /// </summary>
    /// <param name="analysis">The analysis to read.</param>
    /// <returns>The slice.</returns>
    public static SliceModel Slice(this ApplicationModelAnalysis analysis) => analysis.Model.Slices.First();
}
