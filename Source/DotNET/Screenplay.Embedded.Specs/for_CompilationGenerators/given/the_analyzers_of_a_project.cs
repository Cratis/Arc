// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.given;

/// <summary>
/// The source of the analyzers the specifications compile and then load as the analyzers of a project.
/// </summary>
/// <remarks>
/// One of them declares an Arc command, naming it from an additional file and placing it with a build property
/// read out of the analyzer configuration. That is the whole point of it - a document naming that command can only
/// have come from the generator having really run, with really the inputs the build stated.
/// </remarks>
[SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "The authored analyzer sources are explicitly normalized to LF before they are compiled.")]
public static class the_analyzers_of_a_project
{
    /// <summary>
    /// The build property the generated command reads its namespace from.
    /// </summary>
    public const string NamespaceProperty = "build_property.ScreenplayCommandNamespace";

    /// <summary>
    /// What the crashing generator throws with.
    /// </summary>
    public const string Crash = "The generator could not read the application.";

    /// <summary>
    /// A generator declaring an Arc command out of an additional file and a build property.
    /// </summary>
    public static readonly string Generating = """"
        using System.Linq;
        using System.Text;
        using Microsoft.CodeAnalysis;
        using Microsoft.CodeAnalysis.Text;

        namespace Generators;

        [Generator]
        public class CommandGenerator : IIncrementalGenerator
        {
            public void Initialize(IncrementalGeneratorInitializationContext context)
            {
                var names = context.AdditionalTextsProvider
                    .Where(_ => _.Path.EndsWith("commands.txt"))
                    .Select((text, token) => text.GetText(token)!.ToString().Trim());

                var namespaces = context.AnalyzerConfigOptionsProvider.Select((options, _) =>
                    options.GlobalOptions.TryGetValue("build_property.ScreenplayCommandNamespace", out var value) ? value : "Unconfigured");

                context.RegisterSourceOutput(names.Combine(namespaces), (production, stated) =>
                    production.AddSource("Commands.g.cs", SourceText.From($$"""
                        using Cratis.Arc.Commands.ModelBound;

                        namespace {{stated.Right}};

                        [Command]
                        public record {{stated.Left}}(string Title)
                        {
                            public void Handle() { }
                        }
                        """, Encoding.UTF8)));
            }
        }
        """".ReplaceLineEndings("\n");

    /// <summary>
    /// A generator that throws while generating.
    /// </summary>
    public static readonly string Crashing = """
        using Microsoft.CodeAnalysis;

        namespace Generators;

        [Generator]
        public class CrashingGenerator : IIncrementalGenerator
        {
            public void Initialize(IncrementalGeneratorInitializationContext context) =>
                context.RegisterSourceOutput(
                    context.CompilationProvider,
                    (production, compilation) => throw new System.InvalidOperationException("The generator could not read the application."));
        }
        """.ReplaceLineEndings("\n");

    /// <summary>
    /// An analyzer assembly declaring no generator at all, the way a pure analyzer package does.
    /// </summary>
    public static readonly string Analyzing = """
        namespace Generators;

        public class NothingToGenerate
        {
        }
        """.ReplaceLineEndings("\n");
}
