// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Diagnoses invalid operation conventions and bare operation collections in command return shapes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandOperationAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor _signature = new(
        "ARC0016", "Invalid command operation method", "Operation '{0}' requires exactly one public nongeneric instance Execute and optional Compensate returning void, Task, or ValueTask, with required service parameters and no async void, ref parameters, or service locator", "Arc", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor _collection = new(
        "ARC0017", "Use CommandOperations for operation batches", "Command return type '{0}' contains a bare operation collection; use CommandOperations to declare a server-only batch", "Arc", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor _visibility = new(
        "ARC0018", "Operation cannot have a generated invoker", "Operation '{0}' must be a nongeneric public or internal type in accessible nongeneric containing types", "Arc", DiagnosticSeverity.Error, true);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_signature, _collection, _visibility];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (CommandOperationConvention.IsOperation(type) && !type.IsAbstract && type.TypeKind != TypeKind.Interface)
        {
            var execute = CommandOperationConvention.Methods(type, "Execute");
            var compensate = CommandOperationConvention.Methods(type, "Compensate");
            if (execute.Length != 1 || !CommandOperationConvention.IsValid(execute[0], false) ||
                compensate.Length > 1 || compensate.Any(method => !CommandOperationConvention.IsValid(method, true)))
            {
                context.ReportDiagnostic(Diagnostic.Create(_signature, type.Locations[0], type.Name));
            }

            if (!CommandOperationConvention.IsAccessible(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(_visibility, type.Locations[0], type.Name));
            }
        }

        if (type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "Cratis.Arc.Commands.ModelBound.CommandAttribute"))
        {
            foreach (var handle in type.GetMembers("Handle").OfType<IMethodSymbol>().Where(method => HasBareCollection(method.ReturnType)))
            {
                context.ReportDiagnostic(Diagnostic.Create(_collection, handle.Locations[0], handle.ReturnType.ToDisplayString()));
            }
        }
    }

    static bool HasBareCollection(ITypeSymbol type)
    {
        if (type.ToDisplayString() == CommandOperationConvention.Batch)
        {
            return false;
        }

        if (type is IArrayTypeSymbol array)
        {
            return CommandOperationConvention.IsOperation(array.ElementType);
        }

        if (type is INamedTypeSymbol named)
        {
            var contracts = named.AllInterfaces.Add(named);
            if (contracts.Any(contract => contract.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
                CommandOperationConvention.IsOperation(contract.TypeArguments[0])))
            {
                return true;
            }

            return named.TypeArguments.Any(HasBareCollection);
        }

        return false;
    }
}
