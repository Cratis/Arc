// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Generates the Screenplay documents a project embeds, as part of building it.
/// </summary>
/// <remarks>
/// A source generator cannot add a manifest resource, and a step that ran after the build would have to rewrite an
/// assembly that was already produced, signed and consumed. So the documents are generated from the same inputs
/// the C# compiler is about to be given, before resource names are prepared, and the compiler embeds them the way
/// it embeds every other resource of the project.
/// <para>
/// The task answers false the moment anything was reported as an error, and writes nothing in that case. An
/// assembly that shipped with a document the Screenplay compiler rejects would turn a build problem into a runtime
/// one, which is the outcome this exists to make impossible.
/// </para>
/// </remarks>
public class GenerateEmbeddedScreenplayDocuments : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the files being compiled.
    /// </summary>
    [Required]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MSBuild requires item-list task parameters to use ITaskItem arrays.")]
    public ITaskItem[] Sources { get; set; } = [];

    /// <summary>
    /// Gets or sets the assemblies being referenced.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MSBuild requires item-list task parameters to use ITaskItem arrays.")]
    public ITaskItem[] References { get; set; } = [];

    /// <summary>
    /// Gets or sets the analyzers and source generators passed to the compiler.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MSBuild requires item-list task parameters to use ITaskItem arrays.")]
    public ITaskItem[] Analyzers { get; set; } = [];

    /// <summary>
    /// Gets or sets additional files passed to source generators.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MSBuild requires item-list task parameters to use ITaskItem arrays.")]
    public ITaskItem[] AdditionalFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the compiler's analyzer configuration files.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MSBuild requires item-list task parameters to use ITaskItem arrays.")]
    public ITaskItem[] AnalyzerConfigFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the preprocessor symbols the source is compiled with.
    /// </summary>
    public string? DefineConstants { get; set; }

    /// <summary>
    /// Gets or sets the language version the source is compiled with.
    /// </summary>
    public string? LanguageVersion { get; set; }

    /// <summary>
    /// Gets or sets the compiler output type, including executable projects with top-level statements.
    /// </summary>
    public string? OutputType { get; set; }

    /// <summary>
    /// Gets or sets the name of the assembly being built.
    /// </summary>
    [Required]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the document hierarchy is resolved relative to.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="AssemblyName"/>, which is what the SDK defaults the project property to.
    /// </remarks>
    public string? RootNamespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether authoring-only constructs are emitted.
    /// </summary>
    public bool AuthoringOnlyConstructs { get; set; }

    /// <summary>
    /// Gets or sets the directory the generated files are written to.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets the files to embed, each carrying the logical name it is embedded under.
    /// </summary>
    [Output]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MSBuild requires item-list task outputs to use ITaskItem arrays.")]
    public ITaskItem[] Documents { get; private set; } = [];

    /// <inheritdoc/>
    public override bool Execute()
    {
        try
        {
            return Generate();
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception, showStackTrace: true, showDetail: true, file: null);

            return false;
        }
    }

    /// <summary>
    /// Turns a written file into the item the build embeds.
    /// </summary>
    /// <param name="file">The file to embed.</param>
    /// <returns>The <see cref="ITaskItem"/>.</returns>
    static ITaskItem ToItem(EmbeddedResourceFile file)
    {
        var item = new TaskItem(file.Path);
        item.SetMetadata("LogicalName", file.LogicalName);
        item.SetMetadata("ManifestResourceName", file.LogicalName);
        item.SetMetadata("Type", "Non-Resx");

        return item;
    }

    /// <summary>
    /// Generates the documents and writes them.
    /// </summary>
    /// <returns>True when every document was generated and written, false otherwise.</returns>
    bool Generate()
    {
        var compilation = SourceCompilation.Create(
            AssemblyName,
            Sources.Select(_ => _.GetMetadata("FullPath")),
            References.Select(_ => _.GetMetadata("FullPath")),
            DefineConstants,
            LanguageVersion,
            OutputType);

        var generated = CompilationGenerators.Run(
            compilation,
            Analyzers.Select(_ => _.GetMetadata("FullPath")),
            AdditionalFiles.Select(_ => _.GetMetadata("FullPath")),
            AnalyzerConfigFiles.Select(_ => _.GetMetadata("FullPath")));
        var reporter = new BuildDiagnosticReporter(Log);
        foreach (var diagnostic in generated.Diagnostics)
        {
            reporter.Report(diagnostic);
        }

        if (Log.HasLoggedErrors)
        {
            return false;
        }

        var generation = new EmbeddedDocumentGenerator().Generate(generated.Compilation, new(AssemblyName, RootNamespace)
        {
            AuthoringOnlyConstructs = AuthoringOnlyConstructs
        });
        foreach (var diagnostic in generation.Diagnostics)
        {
            reporter.Report(diagnostic);
        }

        if (!generation.IsSuccess || Log.HasLoggedErrors)
        {
            return false;
        }

        var files = GeneratedFiles.Write(OutputPath, generation);

        Documents = [.. files.Select(ToItem)];

        Log.LogMessage(
            MessageImportance.Normal,
            "Embedded {0} Screenplay document(s) for {1}.",
            generation.Documents.Count,
            AssemblyName);

        return true;
    }
}
