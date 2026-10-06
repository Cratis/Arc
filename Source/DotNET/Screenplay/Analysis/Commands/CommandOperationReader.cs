// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Reads operations actually returned by a straight-line command handler.</summary>
/// <param name="types">The type registry.</param>
/// <param name="paths">The source paths.</param>
/// <param name="diagnostics">The diagnostic sink.</param>
public class CommandOperationReader(TypeRegistry types, SourcePaths paths, ScreenplayDiagnostics diagnostics)
{
    static readonly string[] _frameworkNamespaces = ["System", "Microsoft", "Cratis.Arc", "Cratis.Chronicle"];

    /// <summary>Determines whether a type implements the command operation contract.</summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Whether it is an operation.</returns>
    public static bool IsOperation(ITypeSymbol type) => type.Is("Cratis.Arc.Commands.ICommandOperation") || type.FindInterface("Cratis.Arc.Commands.ICommandOperation") is not null;

    /// <summary>Reads a returned operation or literal operation batch.</summary>
    /// <param name="expression">The returned expression.</param>
    /// <param name="model">Its semantic model.</param>
    /// <param name="command">The owning command.</param>
    /// <param name="sources">The generated sources.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns>The fully readable operations.</returns>
    public IEnumerable<OperationModel> Read(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol command, AuthoringSources sources, string location)
    {
        if (expression is CollectionExpressionSyntax collection)
        {
            foreach (var element in collection.Elements)
            {
                if (element is ExpressionElementSyntax item)
                {
                    foreach (var operation in Read(item.Expression, model, command, sources, location))
                    {
                        yield return operation;
                    }
                }
                else
                {
                    Report("An operation batch contains a spread whose contents cannot be read", location);
                }
            }

            yield break;
        }

        if (expression is not BaseObjectCreationExpressionSyntax { Initializer: null } creation ||
            model.GetTypeInfo(creation).Type is not INamedTypeSymbol { IsRecord: true } type || !IsOperation(type) ||
            model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor || creation.ArgumentList is not { } arguments)
        {
            Report("The returned operation is not a directly constructed record and was left in code", location);
            yield break;
        }

        var execute = type.GetMembers("Execute").OfType<IMethodSymbol>().Where(method => !method.IsStatic && method.DeclaredAccessibility == Accessibility.Public).ToArray();
        if (execute is not [var method])
        {
            Report($"Operation '{type.Name}' has no unique public Execute method", location);
            yield break;
        }

        var systems = method.Parameters.Select(parameter => parameter.Type).Where(IsSystem).Distinct<ITypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        if (systems is not [var system])
        {
            Report($"Operation '{type.Name}' uses {systems.Length} external systems; the current grammar requires exactly one", location);
            yield break;
        }

        var compensate = type.GetMembers("Compensate").OfType<IMethodSymbol>().Where(candidate => !candidate.IsStatic && candidate.DeclaredAccessibility == Accessibility.Public).ToArray();
        if (compensate.Length > 1 || compensate.SelectMany(candidate => candidate.Parameters).Select(parameter => parameter.Type)
            .Any(dependency => IsSystem(dependency) && !SymbolEqualityComparer.Default.Equals(dependency, system)))
        {
            Report($"Operation '{type.Name}' has ambiguous compensation or compensation uses another system", location);
            yield break;
        }

        var inputs = new List<PropertyModel>();
        var mappings = new List<PropertyMappingModel>();
        for (var index = 0; index < arguments.Arguments.Count; index++)
        {
            var argument = arguments.Arguments[index];
            var parameter = argument.NameColon is { } named ? constructor.Parameters.FirstOrDefault(parameter => parameter.Name == named.Name.Identifier.ValueText) : constructor.Parameters.ElementAtOrDefault(index);
            var source = MappingSourceReader.ReadPath(argument.Expression, model, command) ?? sources.ReadPath(argument.Expression, model);
            if (parameter is null || source is null || !SymbolEqualityComparer.Default.Equals(parameter.Type, model.GetTypeInfo(argument.Expression).Type))
            {
                Report($"Operation '{type.Name}' has an input not readable from command properties or generated values", location);
                yield break;
            }

            inputs.Add(new(parameter.Name, types.ResolveCarried(parameter.Type)));
            mappings.Add(new(parameter.Name, new PropertyPathSource(source)));
        }

        if (inputs.Count != constructor.Parameters.Length)
        {
            Report($"Operation '{type.Name}' has omitted constructor inputs and was left in code", location);
            yield break;
        }

        var path = paths.Relative(type.SourceFilePath());
        if (!Portable(path))
        {
            Report($"Operation '{type.Name}' has no portable source path; its implementation attachment was left out", location);
            path = null;
        }

        var name = system.Name.Length > 1 && system.Name[0] == 'I' && char.IsUpper(system.Name[1]) ? system.Name[1..] : system.Name;
        if (!ScreenplayIdentifier.IsBareIdentifier(name) || !ScreenplayIdentifier.IsBareIdentifier(type.Name))
        {
            Report($"Operation '{type.Name}' or system '{name}' has a name the grammar cannot hold", location);
            yield break;
        }

        yield return new(type.Name, name, Documentation.SummaryOf(type), inputs, mappings, path, compensate.Length == 1)
        {
            SystemTypeIdentity = system.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        };
    }

    static bool IsSystem(ITypeSymbol type)
    {
        var ns = type.ContainingNamespace.ToDisplayString();
        return type.TypeKind == TypeKind.Interface && !Array.Exists(_frameworkNamespaces, prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal));
    }

    static bool Portable(string? path) => path is not null && !Path.IsPathRooted(path) && !path.Contains(':', StringComparison.Ordinal) &&
        !path.Split('/').Contains("..", StringComparer.Ordinal);

    void Report(string message, string location) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandOperation, message, location);
}
