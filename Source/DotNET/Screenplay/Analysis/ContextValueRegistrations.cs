// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Finds the availability gates and request-scoped values the host registers in code.
/// </summary>
/// <remarks>
/// A value resolved from a read model query by a literal name and a literal key is recovered, and so is nothing else:
/// a gate registered with <c>Gate</c> or <c>GateNamespace</c>, and a value resolved from the host, an expression or
/// code, are composed at run time and reported as not representable.
/// </remarks>
public static class ContextValueRegistrations
{
    const string Declare = "Declare";
    const string GatePart = "Gate";
    const string GateNamespace = "GateNamespace";

    /// <summary>
    /// Reads the registrations of the compilations.
    /// </summary>
    /// <param name="compilations">The compilations to read.</param>
    /// <param name="diagnostics">Where what cannot be recovered is reported.</param>
    /// <returns>The values that map onto a <c>provide</c> line.</returns>
    public static IReadOnlyList<ContextValueModel> In(IEnumerable<Compilation> compilations, ScreenplayDiagnostics diagnostics)
    {
        var values = new List<ContextValueModel>();
        foreach (var compilation in compilations)
        {
            foreach (var tree in compilation.SyntaxTrees.OrderBy(_ => _.FilePath, StringComparer.Ordinal))
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (model.GetOperation(invocation) is IInvocationOperation operation)
                    {
                        Collect(operation, tree.FilePath, values, diagnostics);
                    }
                }
            }
        }

        return values;
    }

    static void Collect(IInvocationOperation operation, string location, List<ContextValueModel> values, ScreenplayDiagnostics diagnostics)
    {
        var method = operation.TargetMethod;
        if (method.Name == Declare && method.ContainingType.Is(WellKnownTypeNames.ContextValueCatalog))
        {
            var (value, reason) = Read(operation, location);
            if (value is not null)
            {
                values.Add(value);
            }
            else
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                    $"A provided context value was left out: {reason}",
                    location);
            }
        }
        else if ((string.Equals(method.Name, GatePart, StringComparison.Ordinal) || string.Equals(method.Name, GateNamespace, StringComparison.Ordinal)) && method.ContainingType.Is(WellKnownTypeNames.ArcAvailabilityOptions))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                $"A gate registered with {method.Name} is composed in code and was left out; gate the part with [AvailableFor] or [AvailableWith] to have it stated in the model",
                location);
        }
    }

    static (ContextValueModel? Value, string Reason) Read(IInvocationOperation operation, string location)
    {
        if (Unwrap(operation.Arguments.FirstOrDefault()?.Value) is not IObjectCreationOperation declaration || declaration.Arguments.Length < 3)
        {
            return (null, "the declaration is not written as a ContextValueDeclaration with its arguments");
        }

        var arguments = declaration.Arguments.OrderBy(_ => _.Parameter?.Ordinal ?? int.MaxValue).Select(_ => Unwrap(_.Value)).ToList();
        if (arguments[0].ConstantValue.Value is not string name || name.Length == 0)
        {
            return (null, "its name is not a literal");
        }

        if (arguments[1] is not IObjectCreationOperation provider || !provider.Type.Is(WellKnownTypeNames.ReadModelContextValueProvider))
        {
            return (null, $"'{name}' is not resolved from a read model query, and the model cannot state a type for a value that the host, an expression or code provides");
        }

        var provided = provider.Arguments.OrderBy(_ => _.Parameter?.Ordinal ?? int.MaxValue).Select(_ => Unwrap(_.Value)).ToList();
        if (provided.Count < 3 || provided[1].ConstantValue.Value is not string query || provided[2].ConstantValue.Value is not string key)
        {
            return (null, $"'{name}' names its query or its key with something other than a literal");
        }

        if (!key.StartsWith("arguments.", StringComparison.Ordinal) && !key.StartsWith("command.", StringComparison.Ordinal))
        {
            return (null, $"'{name}' is keyed by '{key}', which is not a command or query input");
        }

        if (ScopeOf(arguments[2]) is not { } scope)
        {
            return (null, $"'{name}' is declared at a scope that is not written as ContextScope.Application or ContextScope.Of with literal names");
        }

        var required = arguments.Count < 4 || arguments[3].ConstantValue.Value is not false;

        return (new(name, query, key, scope, required, location), string.Empty);
    }

    static IReadOnlyList<string>? ScopeOf(IOperation scope)
    {
        if (IsApplication(scope))
        {
            return [];
        }

        if (scope is not IInvocationOperation invocation || !string.Equals(invocation.TargetMethod.Name, "Of", StringComparison.Ordinal) ||
            Unwrap(invocation.Arguments.FirstOrDefault()?.Value) is not IArrayCreationOperation { Initializer: { } initializer })
        {
            return null;
        }

        var names = initializer.ElementValues.Select(_ => Unwrap(_).ConstantValue.Value as string).ToList();

        return names.Exists(string.IsNullOrEmpty) ? null : [.. names.OfType<string>()];
    }

    static bool IsApplication(IOperation scope) => scope switch
    {
        IPropertyReferenceOperation property => string.Equals(property.Property.Name, "Application", StringComparison.Ordinal),
        IFieldReferenceOperation field => string.Equals(field.Field.Name, "Application", StringComparison.Ordinal),
        _ => false
    };

    static IOperation Unwrap(IOperation? operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation!;
    }
}
