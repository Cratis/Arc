// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Validates consistency declarations using the same lookup and precedence rules as the runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandAttributeAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor _profileConflict = new("ARCCHR0029", "Conflicting command profile", "Command '{0}' has conflicting protection declarations: {1}", "Arc.Chronicle", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor _readConflict = new("ARCCHR0030", "Conflicting read policy", "Command '{0}' declares both Unprotected and Consistency at one read-policy level", "Arc.Chronicle", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor _invalidLookup = new("ARCCHR0031", "Invalid read-model lookup", "LookUpBy on '{0}' requires a read-model parameter and an existing command property", "Arc.Chronicle", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor _inertDeclaration = new("ARCCHR0032", "Inert consistency declaration", "Declaration on protected command '{0}' is inert: {1}", "Arc.Chronicle", DiagnosticSeverity.Info, true);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_profileConflict, _readConflict, _invalidLookup, _inertDeclaration];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var backedTypes = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(() => CommandDecisionReadAnalyzer.FindBackedTypes(start.Compilation));
            start.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            start.RegisterSymbolAction(_ => AnalyzeParameter(_, backedTypes), SymbolKind.Parameter);
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var profile = CommandAttributeSymbols.ResolveCommand(type);
        if (profile.Conflict is { } conflict)
        {
            context.ReportDiagnostic(Diagnostic.Create(_profileConflict, type.Locations.FirstOrDefault(), type.Name, conflict));
        }

        ReportInert(context, type, type);
    }

    static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.Name is not ("Handle" or "Provide"))
        {
            return;
        }

        var command = method.ContainingType;
        ReportInert(context, command, method);
        ReportReadConflict(context, command, CommandAttributeSymbols.ResolveRead(command, command, method).Conflict);
    }

    static void AnalyzeParameter(SymbolAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.ContainingSymbol is not IMethodSymbol method)
        {
            return;
        }

        var command = method.ContainingType;
        if (CommandAttributeSymbols.Find(parameter, AttributePrecedencePolicy.LookUpByName) is not null &&
            CommandAttributeSymbols.ResolveLookup(command, parameter, CommandDecisionReadAnalyzer.IsReadModel(parameter.Type, backedTypes)).Conflict is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(_invalidLookup, parameter.Locations.FirstOrDefault(), command.Name));
        }

        if (method.Name is not ("Handle" or "Provide"))
        {
            return;
        }

        ReportInert(context, command, parameter);
        if (parameter.Type is INamedTypeSymbol model)
        {
            ReportReadConflict(context, command, CommandAttributeSymbols.ResolveRead(command, model, method, parameter).Conflict);
        }
    }

    static void ReportReadConflict(SymbolAnalysisContext context, INamedTypeSymbol command, string? conflict)
    {
        if (conflict is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(_readConflict, context.Symbol.Locations.FirstOrDefault(), command.Name));
        }
    }

    static void ReportInert(SymbolAnalysisContext context, INamedTypeSymbol command, ISymbol declaration)
    {
        if (CommandAttributeSymbols.ResolveCommand(command).Mode != 1)
        {
            return;
        }

        if (declaration.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == AttributePrecedencePolicy.ConsistencyName))
        {
            context.ReportDiagnostic(Diagnostic.Create(_inertDeclaration, declaration.Locations.FirstOrDefault(), command.Name, "Consistency does not change protected decision reads"));
        }

        if (declaration is INamedTypeSymbol && declaration.GetAttributes().Any(_ =>
            _.AttributeClass?.ToDisplayString() == AttributePrecedencePolicy.KeyGuardName && _.ConstructorArguments.FirstOrDefault().Value is 0))
        {
            context.ReportDiagnostic(Diagnostic.Create(_inertDeclaration, declaration.Locations.FirstOrDefault(), command.Name, "KeyGuard Off is redundant"));
        }
    }
}
