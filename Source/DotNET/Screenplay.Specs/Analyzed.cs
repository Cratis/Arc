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
    /// Compiles source referencing a package and recovers the model it describes.
    /// </summary>
    /// <param name="package">The package the source references.</param>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    public static ApplicationModelAnalysis SourceReferencing(MetadataReference package, params (string Path, string Text)[] sources) =>
        new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None)
            .Analyze(Compile([package], sources), new ScreenplayOptions().WithDefaults(AssemblyName));

    /// <summary>
    /// Compiles source into the assembly image a referenced package really is.
    /// </summary>
    /// <param name="name">The name of the assembly.</param>
    /// <param name="text">The source.</param>
    /// <returns>The <see cref="MetadataReference"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the source of the package does not compile.</exception>
    /// <remarks>
    /// The image is emitted rather than referenced as a compilation, because a package the application depends on is
    /// metadata with no syntax tree behind it - which is the whole reason nothing in the compilation declares what it
    /// holds.
    /// </remarks>
    public static MetadataReference Package(string name, string text)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(documentationMode: DocumentationMode.Parse))],
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        if (!emitted.Success)
        {
            throw new InvalidOperationException($"The package '{name}' did not compile - {emitted.Diagnostics.First(_ => _.Severity == DiagnosticSeverity.Error)}");
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

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
    public static Compilation Compile(params (string Path, string Text)[] sources) => Compile([], sources);

    /// <summary>
    /// Compiles source referencing further packages into a compilation.
    /// </summary>
    /// <param name="packages">The packages the source references beyond the platform.</param>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation Compile(IEnumerable<MetadataReference> packages, params (string Path, string Text)[] sources) =>
        CSharpCompilation.Create(
            AssemblyName,
            sources.Append(Root).Select(_ => CSharpSyntaxTree.ParseText(
                _.Text,
                new CSharpParseOptions(documentationMode: DocumentationMode.Parse),
                path: _.Path)),
            _references.Concat(packages),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    /// <summary>
    /// Compiles source into one project of an application written as several.
    /// </summary>
    /// <param name="name">The name of the assembly the project builds.</param>
    /// <param name="references">What the project references beyond the platform, its sibling projects included.</param>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    /// <remarks>
    /// Nothing is added to what the specification declares, unlike the compilations built around a single file, which
    /// carry a root of their own so that paths in the document start somewhere sensible. A project of a real
    /// application already has its own root, and a specification about several of them has to say where each one is
    /// written or there is nothing for the paths of the document to be relative to.
    /// </remarks>
    public static Compilation Project(
        string name,
        IEnumerable<MetadataReference> references,
        params (string Path, string Text)[] sources) =>
        CSharpCompilation.Create(
            name,
            sources.Select(_ => CSharpSyntaxTree.ParseText(
                _.Text,
                new CSharpParseOptions(documentationMode: DocumentationMode.Parse),
                path: _.Path)),
            _references.Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    /// <summary>
    /// Recovers the model the projects of an application describe together.
    /// </summary>
    /// <param name="compilations">The projects, in whatever order the specification hands them over.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    /// <remarks>
    /// No name is offered, which is what a host generating from several projects without configuring one does - no
    /// single assembly names an application written as several.
    /// </remarks>
    public static ApplicationModelAnalysis Projects(params Compilation[] compilations) =>
        new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None)
            .Analyze(compilations, new ScreenplayOptions().WithDefaults(null));

    /// <summary>
    /// Gets everything the compiler itself reported about a compilation.
    /// </summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <returns>The errors, empty when the source compiles.</returns>
    public static IEnumerable<string> ErrorsIn(Compilation compilation) =>
        compilation
            .GetDiagnostics()
            .Where(_ => _.Severity == DiagnosticSeverity.Error)
            .Select(_ => _.ToString());

    /// <summary>
    /// Gets everything the compiler itself reported, so that a specification never asserts against broken source.
    /// </summary>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The errors, empty when the source compiles.</returns>
    public static IEnumerable<string> ErrorsIn(params (string Path, string Text)[] sources) => ErrorsIn([], sources);

    /// <summary>
    /// Gets everything the compiler itself reported for source referencing a package.
    /// </summary>
    /// <param name="package">The package the source references.</param>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The errors, empty when the source compiles.</returns>
    public static IEnumerable<string> ErrorsIn(MetadataReference package, params (string Path, string Text)[] sources) =>
        ErrorsIn([package], sources);

    /// <summary>
    /// Gets everything the compiler itself reported for source referencing further packages.
    /// </summary>
    /// <param name="packages">The packages the source references beyond the platform.</param>
    /// <param name="sources">The source files, keyed by the path each one is compiled as.</param>
    /// <returns>The errors, empty when the source compiles.</returns>
    public static IEnumerable<string> ErrorsIn(IEnumerable<MetadataReference> packages, params (string Path, string Text)[] sources) =>
        Compile(packages, sources)
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
