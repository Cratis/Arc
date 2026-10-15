// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Validates read-protection declarations using the shared runtime declaration policy.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARCCHR0029_ProtectedAndUnprotected,
         DiagnosticDescriptors.ARCCHR0029_ReadProtectionAndUnprotected,
         DiagnosticDescriptors.ARCCHR0031_MissingLookupProperty,
         DiagnosticDescriptors.ARCCHR0031_LookupOnNonReadModel,
         DiagnosticDescriptors.ARCCHR0031_LookupKeyTypeMismatch,
         DiagnosticDescriptors.ARCCHR0032_InertReadProtection];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var backedTypes = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(() => ChronicleStateSymbols.BackedTypes(start.Compilation));
            start.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(_ => AnalyzeParameter(_, backedTypes), SymbolKind.Parameter);
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var profile = CommandAttributeSymbols.ResolveCommand(type);
        if (profile.Conflict == AttributePrecedencePolicy.ProtectionConflict)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0029_ProtectedAndUnprotected, type.Locations.FirstOrDefault(), type.Name));
        }
        else if (profile.Conflict == AttributePrecedencePolicy.ReadProtectionConflict)
        {
            var mode = CommandAttributeSymbols.Find(type, AttributePrecedencePolicy.ReadProtectionName)?.ConstructorArguments.FirstOrDefault().Value;
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0029_ReadProtectionAndUnprotected, type.Locations.FirstOrDefault(), type.Name, mode is 1 ? "Measure" : "On"));
        }

        var declaration = type.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.ToDisplayString() == AttributePrecedencePolicy.ReadProtectionName);
        if (declaration is null)
        {
            return;
        }

        if (!IsCommand(type))
        {
            // A non-sealed base can carry the supported inherited command declaration.
            if (type.IsSealed || type.IsStatic)
            {
                ReportInert(context, type, "ReadProtection applies only to commands or assemblies");
            }
        }
        else if (profile.Mode == 1 && declaration.ConstructorArguments.FirstOrDefault().Value is 2)
        {
            ReportInert(context, type, "ReadProtection On is redundant on a ProtectedDecision command");
        }
        else if (declaration.ConstructorArguments.FirstOrDefault().Value is 0 &&
                 CommandAttributeSymbols.Find(type.ContainingAssembly, AttributePrecedencePolicy.ReadProtectionName)?.ConstructorArguments.FirstOrDefault().Value is null or 0)
        {
            ReportInert(context, type, "ReadProtection Off is redundant when the assembly is already Off");
        }
    }

    static void AnalyzeParameter(SymbolAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.ContainingSymbol is not IMethodSymbol method)
        {
            return;
        }

        var mayBeModel = MayBeReadModel(parameter.Type, context.Compilation, backedTypes);
        if (parameter.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == AttributePrecedencePolicy.UnprotectedName) && !mayBeModel)
        {
            ReportInert(context, parameter, "Unprotected has no effect on a non-read-model parameter");
        }

        var lookup = CommandAttributeSymbols.Find(parameter, AttributePrecedencePolicy.LookUpByName);
        if (lookup?.ConstructorArguments.FirstOrDefault().Value is not string propertyName)
        {
            return;
        }

        var command = method.ContainingType;
        var resolution = CommandAttributeSymbols.ResolveLookup(command, parameter, mayBeModel);
        if (!mayBeModel)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0031_LookupOnNonReadModel, parameter.Locations.FirstOrDefault(), parameter.Name, parameter.Type.Name));
            return;
        }

        var properties = CommandAttributeSymbols.PublicProperties(command).ToArray();
        if (resolution.Conflict is not null)
        {
            var key = parameter.Type is INamedTypeSymbol candidateModel ? ChronicleStateSymbols.ModelKey(candidateModel, context.Compilation) : null;
            var suggestion = properties.Where(property =>
                    (key is null || SymbolEqualityComparer.Default.Equals(property.Type, key.Type)) &&
                    EditDistance(propertyName, property.Name) <= 2)
                .OrderBy(property => EditDistance(propertyName, property.Name)).ThenBy(property => property.Name, StringComparer.Ordinal).FirstOrDefault()?.Name;
            var diagnosticProperties = ImmutableDictionary<string, string?>.Empty.Add("SuggestedProperty", suggestion);
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0031_MissingLookupProperty, parameter.Locations.FirstOrDefault(), diagnosticProperties, parameter.Name, propertyName, command.Name, suggestion ?? "an existing command property"));
            return;
        }

        var property = properties.First(_ => _.Name == propertyName);
        var modelKey = parameter.Type is INamedTypeSymbol model ? ChronicleStateSymbols.ModelKey(model, context.Compilation) : null;
        if (modelKey is not null && !SymbolEqualityComparer.Default.Equals(property.Type, modelKey.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0031_LookupKeyTypeMismatch, parameter.Locations.FirstOrDefault(), parameter.Name, property.Name, property.Type.Name, parameter.Type.Name, modelKey.Type.Name));
        }
    }

    /// <summary>
    /// Allows unseen domain models because a projection or reducer in another project may back them.
    /// </summary>
    /// <param name="type">The parameter type.</param>
    /// <param name="compilation">The compilation.</param>
    /// <param name="backedTypes">Lazily collected projection and reducer targets.</param>
    /// <returns>Whether the type may be a read model.</returns>
    static bool MayBeReadModel(ITypeSymbol type, Compilation compilation, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes) =>
        ChronicleStateSymbols.IsBacked(type, compilation, backedTypes) || !IsProvablyNotReadModel(type);

    static bool IsProvablyNotReadModel(ITypeSymbol type) =>
        type.SpecialType != SpecialType.None ||
        type.TypeKind is TypeKind.Enum or TypeKind.Delegate or TypeKind.Array or TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.Dynamic ||
        (type.ContainingNamespace?.ToDisplayString() is { } ns && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns == "Microsoft" || ns.StartsWith("Microsoft.", StringComparison.Ordinal)));

    static bool IsCommand(INamedTypeSymbol type) =>
        CommandAttributeSymbols.Find(type, "Cratis.Arc.Commands.ModelBound.CommandAttribute") is not null ||
        RawGuidResponseAnalysis.HandleMethods(type).Any();

    static void ReportInert(SymbolAnalysisContext context, ISymbol symbol, string reason) =>
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0032_InertReadProtection, symbol.Locations.FirstOrDefault(), symbol.Name, reason));

    static int EditDistance(string left, string right)
    {
        var row = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var diagonal = row[0];
            row[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var previous = row[j];
                row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), diagonal + (left[i - 1] == right[j - 1] ? 0 : 1));
                diagonal = previous;
            }
        }

        return row[right.Length];
    }
}
