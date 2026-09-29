// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators;

/// <summary>
/// Incremental source generator that lets an executable run the module initializers of its project references without
/// resolving them at runtime.
/// </summary>
/// <remarks>
/// <para>
/// Generated metadata - query metadata, command operation invokers and Fundamentals type discovery providers - is
/// registered from a <c>[ModuleInitializer]</c> in the assembly it describes, and the runtime runs a module initializer
/// only when something first reaches that module. For an executable, this generator emits code that names one type
/// from every project reference, so the runtime can reach each of their modules through <c>typeof(...).Module</c>
/// instead of through the dependency context and <c>Assembly.Load</c>, neither of which works in single-file or
/// trimmed applications.
/// </para>
/// <para>
/// Each type is named in its own lambda, so the runtime resolves each separately and one that cannot be loaded does
/// not stop the others. Types with no interfaces and a base type from the core library are preferred, as they load
/// with the fewest other assemblies. Types in Arc's own generated namespaces are never chosen: the same generated
/// class exists in every executable, so naming one would reach the wrong module.
/// </para>
/// <para>
/// Project references are those the <c>Cratis.Arc.Core</c> build targets report through the
/// <c>CratisArcProjectReferences</c> property. When the property is absent - the targets were not imported - nothing
/// is emitted and Arc falls back to the runtime dependency context.
/// </para>
/// </remarks>
[Generator]
public class ProjectReferenceModulesGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The analyzer config key carrying the project references reported by the build targets.
    /// </summary>
    public const string ProjectReferencesProperty = "build_property.CratisArcProjectReferences";

    /// <summary>
    /// The hint name of the generated source.
    /// </summary>
    public const string HintName = "ProjectReferenceModules.g.cs";

    /// <summary>
    /// The tracking name of the step producing the model the source is generated from.
    /// </summary>
    public const string ModelTrackingName = "ProjectReferenceModules";

    /// <summary>
    /// The name of the generated class; internal rather than file-local so the code compiles with C# 10.
    /// </summary>
    public const string GeneratedClassName = "__CratisArcProjectReferenceModules";

    const string RegistrationTypeName = "Cratis.Arc.ProjectReferenceModuleInitializers";

    static readonly HashSet<string> _excludedAttributes = new(StringComparer.Ordinal)
    {
        "System.ObsoleteAttribute",
        "Microsoft.CodeAnalysis.EmbeddedAttribute",
        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute"
    };

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gate on the cheap, equatable inputs first, so a library or a project without the build targets never
        // walks its references.
        var projectReferences = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            options.GlobalOptions.TryGetValue(ProjectReferencesProperty, out var value) ? ParseProjectReferences(value) : null);
        var isExecutable = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.Options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication or OutputKind.WindowsRuntimeApplication);
        var reportedForExecutable = projectReferences
            .Combine(isExecutable)
            .Select(static (input, _) => input.Right ? input.Left : null);

        var model = reportedForExecutable
            .Combine(context.CompilationProvider)
            .Select(static (input, _) => input.Left is null ? null : CreateModel(input.Right, input.Left))
            .WithTrackingName(ModelTrackingName);

        context.RegisterSourceOutput(model, static (output, modules) =>
        {
            if (modules is not null)
            {
                output.AddSource(HintName, GenerateSource(modules));
            }
        });
    }

    static EquatableArray<string>? ParseProjectReferences(string projectReferences)
    {
        // "<count>|<name>|<name>..." - a list that does not match its count was cut short on the way here, and
        // registering only part of the project references would hide the rest from the runtime fallback.
        var parts = projectReferences.Split('|');
        if (!int.TryParse(parts[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var count))
        {
            return null;
        }

        var names = parts.Skip(1).Select(_ => _.Trim()).Where(_ => _.Length > 0).ToArray();
        return names.Length == count
            ? new(names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(_ => _, StringComparer.Ordinal))
            : null;
    }

    static ProjectReferenceModules? CreateModel(Compilation compilation, EquatableArray<string> reportedNames)
    {
        if (compilation.GetTypeByMetadataName(RegistrationTypeName) is null)
        {
            return null;
        }

        var reported = new HashSet<string>(reportedNames, StringComparer.OrdinalIgnoreCase);
        var unmatched = new HashSet<string>(reportedNames, StringComparer.OrdinalIgnoreCase);
        var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reachable = new List<ReachableProjectReference>();
        var byName = new List<string>();

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly ||
                SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly))
            {
                continue;
            }

            // A project reference is reported by file name, which normally is its assembly name. The IDE can pass it
            // as a compilation reference without a file, so match on either.
            var assemblyName = assembly.Identity.Name;
            var fileName = reference is PortableExecutableReference { FilePath: { } path } ? Path.GetFileNameWithoutExtension(path) : null;
            var matchesAssemblyName = reported.Contains(assemblyName);
            var matchesFileName = fileName is not null && reported.Contains(fileName);
            if (!matchesAssemblyName && !matchesFileName)
            {
                continue;
            }

            unmatched.Remove(assemblyName);
            if (fileName is not null)
            {
                unmatched.Remove(fileName);
            }

            if (!handled.Add(assemblyName))
            {
                continue;
            }

            var type = IsGloballyVisible(reference) ? FindReachableType(compilation, assembly.GlobalNamespace) : null;
            if (type is null)
            {
                byName.Add(assemblyName);
            }
            else
            {
                reachable.Add(new(assemblyName, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }
        }

        // A reported project reference the compilation does not resolve to an assembly is still loaded by name at
        // runtime, and reported if it cannot be, rather than silently left out.
        byName.AddRange(unmatched.Where(_ => !handled.Contains(_)));

        return new(
            new(reachable.OrderBy(_ => _.AssemblyName, StringComparer.Ordinal)),
            new(byName.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(_ => _, StringComparer.Ordinal)));
    }

    static string GenerateSource(ProjectReferenceModules modules)
    {
        var sb = new StringBuilder()
            .AppendLine("// <auto-generated/>")
            .AppendLine("#pragma warning disable")
            .AppendLine("namespace Cratis.Arc.Generated;")
            .AppendLine()
            .AppendLine("/// <summary>")
            .AppendLine("/// Compile-time generated registration of the project reference modules. Do not modify.")
            .AppendLine("/// </summary>")
            .Append("internal static class ").AppendLine(GeneratedClassName)
            .AppendLine("{")
            .AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]")
            .AppendLine("    internal static void Register() =>")
            .AppendLine("        global::Cratis.Arc.ProjectReferenceModuleInitializers.Register(")
            .Append("            typeof(").Append(GeneratedClassName).AppendLine(").Assembly,")
            .AppendLine("            new global::System.Collections.Generic.Dictionary<string, global::System.Func<global::System.Reflection.Module>>")
            .AppendLine("            {");

        // One lambda per project reference: each is compiled on its own, so a type that cannot be resolved fails
        // only its own reference.
        foreach (var reference in modules.Reachable)
        {
            sb.Append("                [").Append(ToStringLiteral(reference.AssemblyName))
                .Append("] = static () => typeof(").Append(reference.TypeName).AppendLine(").Module,");
        }

        return sb
            .AppendLine("            },")
            .Append("            ")
            .Append(modules.ByName.Count == 0
                ? "global::System.Array.Empty<string>()"
                : $"new string[] {{ {string.Join(", ", modules.ByName.Select(ToStringLiteral))} }}")
            .AppendLine(");")
            .AppendLine("}")
            .ToString();
    }

    static bool IsGloballyVisible(MetadataReference reference) =>
        reference.Properties.Aliases.IsDefaultOrEmpty || reference.Properties.Aliases.Contains("global");

    static INamedTypeSymbol? FindReachableType(Compilation compilation, INamespaceSymbol globalNamespace)
    {
        // The cheap syntactic check runs before the symbol lookups of IsReachable, and the search stops at the first
        // type passing both. Types that fail the cheap check are only worth a lookup while there is no fallback yet:
        // the first reachable one is kept, and every later one is skipped without being looked at.
        INamedTypeSymbol? fallback = null;
        foreach (var type in GetOrderedTypes(globalNamespace))
        {
            if (LoadsWithoutOtherAssemblies(type))
            {
                if (IsReachable(compilation, type))
                {
                    return type;
                }
            }
            else if (fallback is null && IsReachable(compilation, type))
            {
                fallback = type;
            }
        }

        return fallback;
    }

    static IEnumerable<INamedTypeSymbol> GetOrderedTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers().OrderBy(_ => _.MetadataName, StringComparer.Ordinal))
        {
            yield return type;
        }

        foreach (var child in @namespace.GetNamespaceMembers().OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            foreach (var type in GetOrderedTypes(child))
            {
                yield return type;
            }
        }
    }

    /// <summary>
    /// Whether loading the type loads no other assembly for its base types or interfaces, which may live in an assembly
    /// only needed at compile time.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>True when it loads without other assemblies.</returns>
    static bool LoadsWithoutOtherAssemblies(INamedTypeSymbol type) =>
        type.AllInterfaces.IsEmpty &&
        (type.TypeKind == TypeKind.Interface ||
         type.BaseType?.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum);

    static bool IsReachable(Compilation compilation, INamedTypeSymbol type) =>
        !type.IsGenericType &&
        type.CanBeReferencedByName &&
        !IsGeneratedByArc(type) &&
        compilation.IsSymbolAccessibleWithin(type, compilation.Assembly) &&
        !type.GetAttributes().Any(_ => _excludedAttributes.Contains(_.AttributeClass?.ToDisplayString() ?? string.Empty)) &&
        SymbolEqualityComparer.Default.Equals(compilation.GetTypeByMetadataName(GetMetadataName(type)), type);

    /// <summary>
    /// Whether the type is one Arc's own generators emit. The registration class this generator emits for an executable
    /// can be visible in a referenced executable through <c>InternalsVisibleTo</c>, and naming it would bind to the
    /// executable's own copy rather than the reference's, so no reference's module is reached through it.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>True when Arc generates it.</returns>
    static bool IsGeneratedByArc(INamedTypeSymbol type)
    {
        if (type.Name == GeneratedClassName)
        {
            return true;
        }

        var @namespace = type.ContainingNamespace.ToDisplayString();
        return @namespace.StartsWith("Cratis.Arc.", StringComparison.Ordinal) &&
            (@namespace.EndsWith(".Generated", StringComparison.Ordinal) || @namespace.IndexOf(".Generated.", StringComparison.Ordinal) >= 0);
    }

    static string GetMetadataName(INamedTypeSymbol type)
    {
        var name = type.MetadataName;
        for (var @namespace = type.ContainingNamespace; !@namespace.IsGlobalNamespace; @namespace = @namespace.ContainingNamespace)
        {
            name = $"{@namespace.MetadataName}.{name}";
        }

        return name;
    }

    static string ToStringLiteral(string value) =>
        $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
