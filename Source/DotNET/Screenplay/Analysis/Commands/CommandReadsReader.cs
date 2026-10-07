// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Reads keyed command dependencies and simple provisioning rejection guards.</summary>
/// <param name="models">The source models.</param>
/// <param name="types">The type registry.</param>
/// <param name="diagnostics">The diagnostic sink.</param>
/// <param name="enabled">Whether authoring reads are enabled.</param>
public class CommandReadsReader(SemanticModels models, TypeRegistry types, ScreenplayDiagnostics diagnostics, bool enabled)
{
    /// <summary>Reads the dependencies Arc resolves by the command event source id.</summary>
    /// <param name="command">The command.</param>
    /// <param name="identifier">The proven command input key.</param>
    /// <param name="sources">The proven source paths.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns>The reads and acceptance requirements.</returns>
    public (IReadOnlyList<CommandReadModel> Reads, IReadOnlyList<CommandRequirementModel> Requirements) Read(
        INamedTypeSymbol command, string? identifier, AuthoringSources sources, string location)
    {
        var methods = command.GetMembers().OfType<IMethodSymbol>().Where(method => !method.IsStatic && (method.Name == "Provide" || method.Name == "Handle")).ToArray();
        var providers = methods.Where(method => method.Name == "Provide").ToArray();
        var parameters = methods.SelectMany(method => method.Parameters).Where(parameter => ReadType(parameter.Type) is not null &&
            (parameter.ContainingSymbol.Name != "Handle" || !providers.Any(provider => Provides(provider.ReturnType, parameter.Type)))).ToArray();
        if (!enabled)
        {
            if (parameters.Length > 0 || methods.Any(method => method.Name == "Provide"))
            {
                Report("Reads and provisioning requirements are authoring-only; enable ScreenplayOptions.AuthoringOnlyConstructs to describe readable dependencies and guards", location);
            }

            return ([], []);
        }

        var reads = new List<CommandReadModel>();
        foreach (var parameter in parameters)
        {
            if (identifier is null || parameter.Type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                Report("The read dependency has no proven command input key or is optional; it was left in code", location);
                continue;
            }

            var type = ReadType(parameter.Type)!;
            var alias = parameter.Name;
            if (command.DeclaredProperties().Any(property => string.Equals(property.Name, alias, StringComparison.OrdinalIgnoreCase)) || alias == "reads" || alias == "as" || alias == "by")
            {
                Report($"The read alias '{alias}' conflicts with command input or a grammar keyword; the dependency was left in code", location);
                continue;
            }

            // Provide and Handle can share a dependency. A returned parameter is carried to Handle, not read twice.
            var existing = reads.Find(read => read.Alias == alias && read.Name == type.Name);
            if (existing is null && reads.Exists(read => read.Alias == alias))
            {
                Report($"The read alias '{alias}' names different models; the dependency was left in code", location);
                continue;
            }

            if (existing is null)
            {
                reads.Add(new(type.Name, alias, identifier)
                {
                    Namespace = type.ContainingNamespace.ToDisplayString(),
                    Properties = new PropertyReader(types).Read(type).ToList()
                });
            }

            if (IsUnchanged(parameter, location))
            {
                sources.Add(parameter, alias, parameter.Type.Is("Cratis.Chronicle.ReadModels.DecisionRead`1"));
            }
        }

        var requirements = new List<CommandRequirementModel>();
        foreach (var provide in methods.Where(method => method.Name == "Provide"))
        {
            var bodies = HandlerBodies.Of(provide).ToArray();
            if (bodies is not [var body] || models.For(body.SyntaxTree) is not { } model)
            {
                Report("Provide() lives in code whose body is unavailable", location);
                continue;
            }

            var recognized = false;
            ExpressionSyntax? provided = null;
            var recovered = new List<CommandRequirementModel>();
            if (body is ConditionalExpressionSyntax conditional)
            {
                var errorOnTrue = ErrorMessage(conditional.WhenTrue, model);
                var errorOnFalse = ErrorMessage(conditional.WhenFalse, model);
                var success = errorOnTrue is not null ? conditional.WhenFalse : conditional.WhenTrue;
                if ((errorOnTrue ?? errorOnFalse) is { } message && sources.ReadPath(success, model) is not null &&
                    Condition(conditional.Condition, model, command, sources, errorOnTrue is not null, location) is { } condition)
                {
                    recovered.Add(new(condition, message));
                    provided = success;
                    recognized = true;
                }
            }
            else if (body is BlockSyntax block && block.Statements.LastOrDefault() is ReturnStatementSyntax { Expression: { } success } &&
                sources.ReadPath(success, model) is not null)
            {
                recognized = true;
                provided = success;
                foreach (var statement in block.Statements.Take(block.Statements.Count - 1))
                {
                    var guard = statement as IfStatementSyntax;
                    var returned = guard?.Statement is BlockSyntax { Statements: [ReturnStatementSyntax error] } ? error : guard?.Statement as ReturnStatementSyntax;
                    if (guard is { Else: null } && returned?.Expression is { } expression && ErrorMessage(expression, model) is { } message &&
                        Condition(guard.Condition, model, command, sources, true, location) is { } condition)
                    {
                        recovered.Add(new(condition, message));
                    }
                    else
                    {
                        recognized = false;
                    }
                }
            }

            if (!recognized)
            {
                Report("Provide() contains provisioning behavior that lives in code; only proven reads are described", location);
                continue;
            }

            requirements.AddRange(recovered);
            if (provided is not null && sources.ReadPath(provided, model) is { } providedPath && !providedPath.Contains('.', StringComparison.Ordinal))
            {
                foreach (var parameter in methods.Where(method => method.Name == "Handle").SelectMany(method => method.Parameters)
                    .Where(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, model.GetTypeInfo(provided).Type)))
                {
                    if (IsUnchanged(parameter, location))
                    {
                        sources.Add(parameter, providedPath);
                    }
                }
            }
        }

        return (reads, requirements);
    }

    static bool Provides(ITypeSymbol result, ITypeSymbol dependency) => SymbolEqualityComparer.Default.Equals(result, dependency) ||
        (result is INamedTypeSymbol named && named.TypeArguments.Any(argument => Provides(argument, dependency)));

    static INamedTypeSymbol? ReadType(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol named when named.Is("Cratis.Chronicle.ReadModels.DecisionRead`1") && named.TypeArguments is [INamedTypeSymbol read] => read,
        INamedTypeSymbol named when named.HasAttribute(WellKnownTypeNames.ReadModelAttribute) => named,
        _ => null
    };

    static string? ErrorMessage(ExpressionSyntax expression, SemanticModel model) => expression is InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } invocation &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && method.Name == "Error" && method.ContainingType.Is("Cratis.Arc.Validation.ValidationResult") &&
        model.GetConstantValue(argument.Expression) is { HasValue: true, Value: string message } ? message : null;

    bool IsUnchanged(IParameterSymbol parameter, string location)
    {
        if (HandlerBodies.Of((IMethodSymbol)parameter.ContainingSymbol).ToArray() is not [var body] || models.For(body.SyntaxTree) is not { } model)
        {
            Report($"The body using read parameter '{parameter.Name}' is unavailable; dependent mappings and requirements were left in code", location);
            return false;
        }

        if (!CommandAuthoringReader.IsUnchanged(parameter, body, model))
        {
            Report($"Read parameter '{parameter.Name}' is reassigned or passed by reference; dependent mappings and requirements were left in code", location);
            return false;
        }

        return true;
    }

    ComparisonCondition? Condition(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol command, AuthoringSources sources, bool invert, string location)
    {
        if (MappingSourceReader.Unwrap(expression) is not BinaryExpressionSyntax comparison)
        {
            return null;
        }

        var left = MappingSourceReader.ReadPath(comparison.Left, model, command) ?? sources.ReadPath(comparison.Left, model);
        var right = MappingSourceReader.ReadPath(comparison.Right, model, command) ?? sources.ReadPath(comparison.Right, model);
        if ((left?.Contains('.', StringComparison.Ordinal) == false && MappingSourceReader.ReadPath(comparison.Left, model, command) is null) ||
            (right?.Contains('.', StringComparison.Ordinal) == false && MappingSourceReader.ReadPath(comparison.Right, model, command) is null))
        {
            return null;
        }

        var constant = model.GetConstantValue(comparison.Right);
        if (EnumConstants.EnumerationOf(comparison.Right, model) is { } enumeration &&
            (!constant.HasValue || !EnumConstants.TryResolve(enumeration, constant.Value, out _)))
        {
            return null;
        }

        var operand = right is not null ? new PropertyPathSource(right) : new MappingSourceReader(diagnostics).Read(comparison.Right, model, command, location);
        var kind = comparison.Kind() switch
        {
            SyntaxKind.EqualsExpression => invert ? ComparisonKind.NotEqual : ComparisonKind.Equal,
            SyntaxKind.NotEqualsExpression => invert ? ComparisonKind.Equal : ComparisonKind.NotEqual,
            SyntaxKind.LessThanExpression => invert ? ComparisonKind.GreaterThanOrEqual : ComparisonKind.LessThan,
            SyntaxKind.LessThanOrEqualExpression => invert ? ComparisonKind.GreaterThan : ComparisonKind.LessThanOrEqual,
            SyntaxKind.GreaterThanExpression => invert ? ComparisonKind.LessThanOrEqual : ComparisonKind.GreaterThan,
            SyntaxKind.GreaterThanOrEqualExpression => invert ? ComparisonKind.LessThan : ComparisonKind.GreaterThanOrEqual,
            _ => (ComparisonKind?)null
        };

        return left is not null && kind is { } op && operand is not null
            ? new(left, op, operand)
            : null;
    }

    void Report(string message, string location) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandProvisioning, message, location);
}
