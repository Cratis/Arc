// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Reads keyed command dependencies and simple provisioning rejection guards.</summary>
/// <param name="models">The source models.</param>
/// <param name="types">The type registry.</param>
/// <param name="diagnostics">The diagnostic sink.</param>
/// <param name="enabled">Whether authoring reads are enabled.</param>
public class CommandReadsReader(SemanticModels models, TypeRegistry types, ScreenplayDiagnostics diagnostics, bool enabled)
{
    /// <summary>
    /// Gets whether every Provide method of the last command read only passes protected reads through its guards.
    /// </summary>
    public bool ProvisionStatesReadsOnly { get; private set; }

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
        var protection = CommandAttributeSymbols.ResolveCommand(command).ReadProtection;
        if (!enabled)
        {
            var protectedParameters = parameters.Where(parameter => IsProtected(parameter, protection)).ToArray();
            if (protectedParameters.Length < parameters.Length || (protectedParameters.Length == 0 && providers.Length > 0))
            {
                Report("Reads and provisioning requirements are authoring-only; enable ScreenplayOptions.AuthoringOnlyConstructs to describe readable dependencies and guards", location);
            }

            parameters = protectedParameters;
            if (parameters.Length == 0)
            {
                return ([], []);
            }
        }

        var guards = methods.SelectMany(method => ProtectedReadGuards.Leading(method, models).Select(guard => (Method: method, Guard: guard))).ToArray();
        var reads = new List<CommandReadModel>();
        var accepted = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var parameter in parameters)
        {
            var type = ReadType(parameter.Type)!;
            var isProtected = IsProtected(parameter, protection);
            var absence = isProtected ? guards.Where(entry => SymbolEqualityComparer.Default.Equals(ProtectedReadGuards.AbsenceOf(entry.Guard.Statement.Condition, entry.Guard.Model), parameter)).Select(entry => entry.Guard).ToArray() : [];
            var (key, failure) = Key(command, parameter, type, protection, identifier, absence.Length > 0);
            failure ??= Conflict(command, parameter.Name, type, key, reads);
            if (failure is not null)
            {
                Refuse(isProtected, failure, location);
                continue;
            }

            // Provide and Handle can share a dependency. A returned parameter is carried to Handle, not read twice.
            if (reads.Find(read => read.Alias == parameter.Name && read.Name == type.Name) is null)
            {
                reads.Add(new(type.Name, parameter.Name, key!)
                {
                    Namespace = type.ContainingNamespace.ToDisplayString(),
                    FullName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(),
                    Properties = new PropertyReader(types).Read(type).ToList(),
                    Protected = isProtected
                });
            }

            if (isProtected)
            {
                accepted.Add(parameter);
                StateAbsence(parameter.Name, absence, sources, location);
            }

            if (IsUnchanged(parameter, location))
            {
                sources.Add(parameter, parameter.Name, parameter.Type.Is("Cratis.Chronicle.ReadModels.DecisionRead`1"));
            }
        }

        var requirements = new List<CommandRequirementModel>();
        var unrecognized = 0;
        foreach (var provide in providers)
        {
            var bodies = HandlerBodies.Of(provide).ToArray();
            if (bodies is not [var body] || models.For(body.SyntaxTree) is not { } model)
            {
                Report("Provide() lives in code whose body is unavailable", location);
                unrecognized++;
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
                    if (guard is not null && sources.ConsumedGuards.Contains(guard))
                    {
                        continue;
                    }

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
                unrecognized++;
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

        requirements.AddRange(HandleRequirements(methods, accepted, command, sources, location));
        ProvisionStatesReadsOnly = providers.Length > 0 && unrecognized == 0 && reads.Exists(read => read.Protected);

        return (reads, requirements);
    }

    /// <summary>
    /// Reads the literal message of a <c>ValidationResult.Error</c> call.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="model">The semantic model of the expression.</param>
    /// <returns>The message, or null when the expression is not such a call.</returns>
    internal static string? ErrorMessage(ExpressionSyntax expression, SemanticModel model) => expression is InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } invocation &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && method.Name == "Error" && method.ContainingType.Is("Cratis.Arc.Validation.ValidationResult") &&
        model.GetConstantValue(argument.Expression) is { HasValue: true, Value: string message } ? message : null;

    static bool IsProtected(IParameterSymbol parameter, int protection) => protection == CommandReadKeys.ProtectionOn &&
        !CommandAttributeSymbols.ResolveRead(ReadType(parameter.Type)!, (IMethodSymbol)parameter.ContainingSymbol, parameter).Advisory;

    static string? Conflict(INamedTypeSymbol command, string alias, INamedTypeSymbol type, string? key, List<CommandReadModel> reads)
    {
        if (command.DeclaredProperties().Any(property => string.Equals(property.Name, alias, StringComparison.OrdinalIgnoreCase)) || alias == "reads" || alias == "as" || alias == "by")
        {
            return $"the read alias '{alias}' conflicts with command input or a grammar keyword";
        }

        return reads.Find(read => read.Alias == alias && (read.Name != type.Name || read.Key != key)) is not null
            ? $"the read alias '{alias}' names different models or keys"
            : null;
    }

    static (string? Key, string? Failure) Key(INamedTypeSymbol command, IParameterSymbol parameter, INamedTypeSymbol type, int protection, string? identifier, bool absenceRejected)
    {
        if (parameter.Type.NullableAnnotation == NullableAnnotation.Annotated && !absenceRejected)
        {
            return (null, "the read is optional and no leading guard rejects its absence");
        }

        return CommandReadKeys.Resolve(command, parameter, type, protection, identifier);
    }

    static bool Provides(ITypeSymbol result, ITypeSymbol dependency) => SymbolEqualityComparer.Default.Equals(result, dependency) ||
        (result is INamedTypeSymbol named && named.TypeArguments.Any(argument => Provides(argument, dependency)));

    static INamedTypeSymbol? ReadType(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol named when named.Is("Cratis.Chronicle.ReadModels.DecisionRead`1") && named.TypeArguments is [INamedTypeSymbol read] => read,
        INamedTypeSymbol named when named.HasAttribute(WellKnownTypeNames.ReadModelAttribute) => named,
        _ => null
    };

    void StateAbsence(string alias, IReadOnlyList<ReadGuard> absence, AuthoringSources sources, string location)
    {
        foreach (var guard in absence)
        {
            sources.ConsumeGuard(guard.Statement);
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnprotectableCommandRead,
                $"Read '{alias}': the model rejects a missing instance itself, so the message '{guard.Message}' is not carried and the evaluator's absence message applies",
                location);
        }
    }

    IEnumerable<CommandRequirementModel> HandleRequirements(
        IMethodSymbol[] methods, HashSet<ISymbol> reads, INamedTypeSymbol command, AuthoringSources sources, string location)
    {
        var guards = methods.Where(method => method.Name == "Handle").SelectMany(method => ProtectedReadGuards.Leading(method, models))
            .Where(guard => !sources.ConsumedGuards.Contains(guard.Statement) &&
                guard.Statement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(identifier => reads.Contains(guard.Model.GetSymbolInfo(identifier).Symbol!)));
        foreach (var guard in guards)
        {
            if (Condition(guard.Statement.Condition, guard.Model, command, sources, true, location) is { } condition)
            {
                sources.ConsumeGuard(guard.Statement);

                yield return new(condition, guard.Message);
            }
            else
            {
                diagnostics.Information(ScreenplayDiagnosticCodes.UnprotectableCommandRead, $"A guard over a protected read ('{guard.Message}') is not a comparison the model states; it was left in code", location);
            }
        }
    }

    void Refuse(bool isProtected, string failure, string location)
    {
        if (isProtected)
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.UnprotectableCommandRead, $"A protected read was left in code, never emitted as an unguarded read: {failure}", location);
            return;
        }

        Report($"The read dependency was left in code: {failure}", location);
    }

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

    ComparisonCondition? DateCondition(BinaryExpressionSyntax binary, DateConditionSite site, bool invert)
    {
        var condition = new ConditionReader(diagnostics).Read(binary, site.Model, (INamedTypeSymbol)site.Owner, site.Location);

        return (invert ? ConditionReader.Invert(condition) : condition) as ComparisonCondition;
    }

    ComparisonCondition? Condition(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol command, AuthoringSources sources, bool invert, string location)
    {
        var site = new DateConditionSite(model, command, location, null, diagnostics);

        return MappingSourceReader.Unwrap(expression) is BinaryExpressionSyntax binary && DateConditions.Applies(binary, model)
            ? DateCondition(binary, site, invert)
            : ReadComparison(expression, sources, site, invert);
    }

    ComparisonCondition? ReadComparison(ExpressionSyntax expression, AuthoringSources sources, DateConditionSite site, bool invert)
    {
        var model = site.Model;
        var command = (INamedTypeSymbol)site.Owner;
        var location = site.Location;
        if (MappingSourceReader.Unwrap(expression) is not BinaryExpressionSyntax comparison ||
            model.GetOperation(comparison) is not IBinaryOperation { OperatorMethod: null } operation ||
            operation.LeftOperand is IConversionOperation || operation.RightOperand is IConversionOperation ||
            !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(comparison.Left).Type, model.GetTypeInfo(comparison.Right).Type))
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
