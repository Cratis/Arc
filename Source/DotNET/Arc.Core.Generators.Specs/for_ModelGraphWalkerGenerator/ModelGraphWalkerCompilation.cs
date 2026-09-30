// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

/// <summary>
/// Compiles model source, with or without the <see cref="ModelGraphWalkerGenerator"/>, the way a consuming project is.
/// </summary>
public static class ModelGraphWalkerCompilation
{
    /// <summary>
    /// Compiles source, optionally runs the generator over it, and checks the result builds.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The model source.</param>
    /// <param name="generate">Whether to run the generator.</param>
    /// <param name="references">Additional references.</param>
    /// <returns>The generated walker source, if any, the warnings and errors outside the model source, and the compilation.</returns>
    public static (string Source, Diagnostic[] Diagnostics, Compilation Output) Compile(string name, string source, bool generate, params MetadataReference[] references) =>
        Compile(name, source, generate, [], references);

    /// <summary>
    /// Compiles source in a project that allows unsafe code, optionally runs the generator over it, and checks the
    /// result builds.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The model source.</param>
    /// <param name="generate">Whether to run the generator.</param>
    /// <param name="references">Additional references.</param>
    /// <returns>The generated walker source, if any, the warnings and errors outside the model source, and the compilation.</returns>
    public static (string Source, Diagnostic[] Diagnostics, Compilation Output) CompileAllowingUnsafe(string name, string source, bool generate, params MetadataReference[] references) =>
        Compile(name, source, generate, [], true, references);

    /// <summary>
    /// Compiles source, runs other generators and optionally the generator over it, and checks the result builds.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The model source.</param>
    /// <param name="generate">Whether to run the generator.</param>
    /// <param name="others">Other generators to run alongside it, as a consuming project can.</param>
    /// <param name="references">Additional references.</param>
    /// <returns>The generated walker source, if any, the warnings and errors outside the model source, and the compilation.</returns>
    public static (string Source, Diagnostic[] Diagnostics, Compilation Output) Compile(string name, string source, bool generate, IIncrementalGenerator[] others, params MetadataReference[] references) =>
        Compile(name, source, generate, others, false, references);

    static (string Source, Diagnostic[] Diagnostics, Compilation Output) Compile(string name, string source, bool generate, IIncrementalGenerator[] others, bool allowUnsafe, MetadataReference[] references)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            References().Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe, nullableContextOptions: NullableContextOptions.Enable));
        var generators = generate ? others.Prepend(new ModelGraphWalkerGenerator()).ToArray() : others;
        if (generators.Length == 0)
        {
            return (string.Empty, [], compilation);
        }

        var driver = CSharpGeneratorDriver.Create(generators)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var generated = driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources)
            .SingleOrDefault(generatedSource => generatedSource.HintName == "ModelGraphWalkers.g.cs");
        var models = compilation.SyntaxTrees.Single();
        Diagnostic[] diagnostics = [.. generatorDiagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning && diagnostic.Location.SourceTree != models)];
        return (generated.HintName is null ? string.Empty : generated.SourceText.ToString(), diagnostics, output);
    }

    /// <summary>
    /// Emits a library as a reference, either as a reference assembly or as its implementation.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The library source.</param>
    /// <param name="referenceAssembly">Whether to emit a reference assembly.</param>
    /// <returns>The <see cref="MetadataReference"/>.</returns>
    /// <exception cref="InvalidOperationException">The library does not compile.</exception>
    public static MetadataReference Library(string name, string source, bool referenceAssembly)
    {
        var (_, _, compilation) = Compile(name, source, false);
        using var binary = new MemoryStream();
        var emitted = compilation.Emit(binary, options: new EmitOptions(metadataOnly: referenceAssembly, includePrivateMembers: !referenceAssembly));
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        }

        return MetadataReference.CreateFromImage(binary.ToArray());
    }

    /// <summary>
    /// Emits a compilation, loads it and runs its module initializers.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The loaded <see cref="Assembly"/>.</returns>
    /// <exception cref="InvalidOperationException">The compilation does not emit.</exception>
    public static Assembly Load(Compilation compilation)
    {
        using var binary = new MemoryStream();
        var emitted = compilation.Emit(binary);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        }

        var assembly = Assembly.Load(binary.ToArray());
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        return assembly;
    }

    /// <summary>
    /// Validates the sample a loaded model assembly creates, recording every node the traversal reaches.
    /// </summary>
    /// <param name="assembly">The loaded model assembly, with a <c>Walked.Samples.Create</c> method.</param>
    /// <returns>The nodes reached, in order, as the node type and its member path.</returns>
    public static string Walk(Assembly assembly)
    {
        var visits = new List<string>();
        var discoverableValidators = Substitute.For<IDiscoverableValidators>();
        var validator = Substitute.For<IValidator>();
        discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = validator;
                return true;
            });

        var validatorInvoker = Substitute.For<IValidatorInvoker>();
        validatorInvoker.Invoke(default!, default!, default!, default)
            .ReturnsForAnyArgs(x =>
            {
                visits.Add($"{x[0].GetType().Name}@{x[2]}");
                return Task.FromResult<IEnumerable<Validation.ValidationResult>>([]);
            });

        var root = assembly.GetType("Walked.Samples")!.GetMethod("Create")!.Invoke(null, null)!;
        new ModelGraphValidator(discoverableValidators, validatorInvoker).Validate(new ModelGraphValidationRequest(root)).GetAwaiter().GetResult();
        return string.Join(" | ", visits);
    }

    /// <summary>
    /// Describes the members walked for every type a loaded assembly registers a walker for.
    /// </summary>
    /// <param name="assembly">The loaded assembly.</param>
    /// <param name="members">Gets the members walked for a type.</param>
    /// <param name="constructed">The constructed generic types to describe as well, as the assembly's types only hold generic definitions.</param>
    /// <returns>One line per registered type with the names of its walked members.</returns>
    internal static string Describe(Assembly assembly, Func<Type, WalkableMember[]> members, params Type[] constructed) => string.Join(
        Environment.NewLine,
        assembly.GetTypes().Concat(constructed)
            .Where(type => ModelGraphWalkers.TryGet(type, out _))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => $"{type}: {string.Join(", ", members(type).Select(member => member.Name))}"));

    static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ModelGraphWalkers).Assembly.Location)
            .Append(typeof(ConceptAs<>).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path));
}
