// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the ability invocations an operation phase makes through its system collaborators.
/// </summary>
/// <param name="types">The type registry.</param>
public class OperationAbilityReader(TypeRegistry types)
{
    const string Marker = "Cratis.Arc.Commands.Abilities.ISystemAbilities";
    const string Async = "Async";

    /// <summary>Determines whether a type is a system collaborator whose methods are abilities.</summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Whether it is.</returns>
    public static bool IsCollaborator(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Interface && (type.Is(Marker) || type.FindInterface(Marker) is not null);

    /// <summary>Gets the system name a collaborator interface stands for: its name without a leading I.</summary>
    /// <param name="collaborator">The collaborator interface.</param>
    /// <returns>The system name.</returns>
    public static string SystemName(ITypeSymbol collaborator) =>
        collaborator.Name.Length > 1 && collaborator.Name[0] == 'I' && char.IsUpper(collaborator.Name[1]) ? collaborator.Name[1..] : collaborator.Name;

    /// <summary>
    /// Reads a phase made only of ability calls, in order. A phase that does anything else is not readable, because the
    /// model would then describe less than the code does.
    /// </summary>
    /// <param name="phase">The Execute or Compensate method.</param>
    /// <param name="operation">The operation type that owns the phase.</param>
    /// <param name="inputs">The operation's inputs, by name.</param>
    /// <param name="compilation">The compilation holding the operation.</param>
    /// <param name="reason">Why the phase cannot be read, when it cannot.</param>
    /// <returns>The invocations, or null when the phase is not readable.</returns>
    public IReadOnlyList<AbilityInvocationModel>? Read(IMethodSymbol phase, INamedTypeSymbol operation, IReadOnlyCollection<string> inputs, Compilation compilation, out string reason)
    {
        reason = string.Empty;
        if (phase.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax declaration)
        {
            reason = $"'{phase.Name}' has no source to read";
            return null;
        }

        var model = compilation.GetSemanticModel(declaration.SyntaxTree);
        var expressions = new List<ExpressionSyntax>();
        if (declaration.ExpressionBody is { } arrow)
        {
            expressions.Add(arrow.Expression);
        }
        else
        {
            foreach (var statement in declaration.Body?.Statements ?? default)
            {
                var expression = statement switch
                {
                    ExpressionStatementSyntax expressionStatement => expressionStatement.Expression,
                    ReturnStatementSyntax { Expression: { } returned } => returned,
                    _ => null
                };
                if (expression is null)
                {
                    reason = $"'{phase.Name}' does something other than call abilities";
                    return null;
                }

                expressions.Add(expression);
            }
        }

        var invocations = new List<AbilityInvocationModel>();
        foreach (var expression in expressions)
        {
            var invocation = expression is AwaitExpressionSyntax awaited ? awaited.Expression : expression;
            if (invocation is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call ||
                model.GetSymbolInfo(access.Expression).Symbol is not IParameterSymbol { Type: var collaborator } receiver ||
                !phase.Parameters.Contains(receiver, SymbolEqualityComparer.Default) || !IsCollaborator(collaborator) ||
                model.GetSymbolInfo(call).Symbol is not IMethodSymbol ability)
            {
                reason = $"'{phase.Name}' does something other than call abilities";
                return null;
            }

            var mappings = new List<PropertyMappingModel>();
            var parameters = new List<PropertyModel>();
            for (var index = 0; index < call.ArgumentList.Arguments.Count; index++)
            {
                var argument = call.ArgumentList.Arguments[index];
                var parameter = argument.NameColon is { } named
                    ? ability.Parameters.FirstOrDefault(candidate => candidate.Name == named.Name.Identifier.ValueText)
                    : ability.Parameters.ElementAtOrDefault(index);
                if (parameter is null)
                {
                    reason = $"'{phase.Name}' calls '{ability.Name}' with an argument that matches no parameter";
                    return null;
                }

                if (parameter.Type.Is("System.Threading.CancellationToken"))
                {
                    continue;
                }

                var symbol = model.GetSymbolInfo(MappingSourceReader.Unwrap(argument.Expression)).Symbol;
                if (symbol is not (IPropertySymbol or IParameterSymbol or IFieldSymbol) || !inputs.Contains(symbol.Name) ||
                    !(SymbolEqualityComparer.Default.Equals(symbol.ContainingType, operation) ||
                      SymbolEqualityComparer.Default.Equals(symbol.ContainingSymbol?.ContainingType, operation)))
                {
                    reason = $"'{phase.Name}' passes '{ability.Name}' a value that is not one of the operation's inputs";
                    return null;
                }

                mappings.Add(new(parameter.Name, new PropertyPathSource(symbol.Name)));
                parameters.Add(new(parameter.Name, types.ResolveCarried(parameter.Type)));
            }

            var name = ability.Name.Length > Async.Length && ability.Name.EndsWith(Async, StringComparison.Ordinal) ? ability.Name[..^Async.Length] : ability.Name;
            var system = SystemName(collaborator);
            if (!ScreenplayIdentifier.IsBareIdentifier(system) || !ScreenplayIdentifier.IsBareIdentifier(name))
            {
                reason = $"'{system}.{name}' has a name the grammar cannot hold";
                return null;
            }

            invocations.Add(new(system, name, mappings, parameters));
        }

        return invocations;
    }
}
