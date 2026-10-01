// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.given;

/// <summary>
/// An application whose commands only exist because a source generator put them there.
/// </summary>
/// <remarks>
/// The analyzers are real assemblies compiled and written to disk for the specification, because what is being
/// specified is that the build loads and runs the generators a project really compiles with. A generator declared
/// in the specification assembly would already be loaded, already be the right Roslyn, and would prove none of it.
/// </remarks>
public sealed class a_generated_application : IDisposable
{
    /// <summary>
    /// The name of the assembly being compiled.
    /// </summary>
    public const string AssemblyName = "Library";

    /// <summary>
    /// The namespace the generated command is declared into, stated only in the analyzer configuration.
    /// </summary>
    public const string CommandNamespace = "Library.Catalog.Books";

    /// <summary>
    /// The name of the generated command, stated only in the additional file.
    /// </summary>
    public const string CommandName = "RegisterBook";

    static readonly MetadataReference[] _references =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => MetadataReference.CreateFromFile(_))
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="a_generated_application"/> class.
    /// </summary>
    public a_generated_application()
    {
        Directory = Path.Combine(Path.GetTempPath(), $"cratis-generators-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(Directory);
    }

    /// <summary>
    /// Gets the directory everything the specification needs on disk lives in.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Gets the analyzer generating the command of the application.
    /// </summary>
    public string Generating => Analyzer("Commands.Generator", the_analyzers_of_a_project.Generating);

    /// <summary>
    /// Gets the analyzer that throws while generating.
    /// </summary>
    public string Crashing => Analyzer("Crashing.Generator", the_analyzers_of_a_project.Crashing);

    /// <summary>
    /// Gets an analyzer that declares nothing to generate, the way a pure analyzer package does.
    /// </summary>
    public string Analyzing => Analyzer("Nothing.Generator", the_analyzers_of_a_project.Analyzing);

    /// <summary>
    /// Gets a file that carries the analyzer extension without being an assembly at all.
    /// </summary>
    public string NotAnAssembly => Written("Broken.Generator.dll", "This is not an assembly.");

    /// <summary>
    /// Gets the additional file naming the command to generate.
    /// </summary>
    public string Commands => Written("commands.txt", CommandName);

    /// <summary>
    /// Gets the analyzer configuration stating the namespace the command is generated into.
    /// </summary>
    public string Configuration => Written(
        "build.globalconfig",
        $"is_global = true{Environment.NewLine}{the_analyzers_of_a_project.NamespaceProperty} = {CommandNamespace}{Environment.NewLine}");

    /// <summary>
    /// Gets the path of a file the build states but nothing ever wrote.
    /// </summary>
    /// <param name="name">The name of the file.</param>
    /// <returns>The path.</returns>
    public string Missing(string name) => Path.Combine(Directory, name);

    /// <summary>
    /// Compiles the authored half of the application - which declares no command of its own.
    /// </summary>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public Compilation Compiled() =>
        CSharpCompilation.Create(
            AssemblyName,
            [CSharpSyntaxTree.ParseText(
                "namespace Library;\n\npublic static class Program\n{\n    public static void Main() { }\n}\n",
                new CSharpParseOptions(LanguageVersion.Latest),
                Path.Combine(Directory, AssemblyName, "Program.cs"))],
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }

    /// <summary>
    /// Compiles an analyzer assembly and writes it where the build would find it.
    /// </summary>
    /// <param name="name">The name of the assembly.</param>
    /// <param name="source">The source of the assembly.</param>
    /// <returns>The path of the assembly.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the analyzer itself does not compile.</exception>
    string Analyzer(string name, string source)
    {
        var path = Path.Combine(Directory, $"{name}.dll");

        if (File.Exists(path))
        {
            return path;
        }

        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var emitted = compilation.Emit(path);

        return emitted.Success
            ? path
            : throw new InvalidOperationException(
                $"The analyzer '{name}' of the specification does not compile:{Environment.NewLine}" +
                string.Join(Environment.NewLine, emitted.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error)));
    }

    /// <summary>
    /// Writes a file of the project.
    /// </summary>
    /// <param name="name">The name of the file.</param>
    /// <param name="content">What the file holds.</param>
    /// <returns>The path of the file.</returns>
    string Written(string name, string content)
    {
        var path = Path.Combine(Directory, name);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
        }

        return path;
    }
}
