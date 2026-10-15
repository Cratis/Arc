// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Checks automatic stream-model bindings and statically visible guard widening.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PerStreamReadModelAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0037_ReadModelStreamNotOnCommandRoute,
        DiagnosticDescriptors.ARCCHR0038_StreamReadGuardWidens
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var backed = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(() => ChronicleStateSymbols.BackedTypes(start.Compilation));
            var validators = new Lazy<IMethodSymbol[]>(() => start.Compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>().SelectMany(type => type.InstanceConstructors)
                .Where(constructor => ReadProtectionSymbols.CommandFor(constructor) is not null).ToArray());
            start.RegisterSymbolAction(symbol => Analyze(symbol, backed, validators), SymbolKind.Parameter);
        });
    }

    static void Analyze(SymbolAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backed, Lazy<IMethodSymbol[]> validators)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.ContainingSymbol is not IMethodSymbol method || method.IsStatic ||
            ReadProtectionSymbols.CommandFor(method) is not { } command ||
            (method.MethodKind != MethodKind.Ordinary && method.MethodKind != MethodKind.Constructor) ||
            (method.MethodKind == MethodKind.Ordinary && method.Name != "Handle" && method.Name != "Provide") ||
            ChronicleStateSymbols.IsKeyedService(parameter, context.Compilation) ||
            ChronicleStateSymbols.MayBeProvided(method, command) ||
            PerStreamReadModelSymbols.Declaration(parameter.Type) is not { } declaration)
        {
            return;
        }
        var source = PerStreamReadModelSymbols.Source(declaration);
        var stream = PerStreamReadModelSymbols.Stream(declaration);
        if (Mismatch(command, parameter, source, stream, context.Compilation) is { } mismatch)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0037_ReadModelStreamNotOnCommandRoute, parameter.Locations.FirstOrDefault(), command.Name, parameter.Name, parameter.Type.Name, source.Name, stream, mismatch));
            return;
        }
        if (ReadProtectionSymbols.Mode(command) == 0 || ReadProtectionSymbols.Advisory(parameter)) return;
        var dependencies = ChronicleStateSymbols.DependencyMethods(command)
            .Where(dependency => !ChronicleStateSymbols.MayBeProvided(dependency, command))
            .Concat(validators.Value.Where(validator => SymbolEqualityComparer.Default.Equals(ReadProtectionSymbols.CommandFor(validator), command)))
            .SelectMany(dependency => dependency.Parameters).ToArray();
        var reason = WidensByDeclaration(source, stream) ? "declared Concurrency omits a stream dimension" :
            dependencies.Any(other => !ReadProtectionSymbols.Advisory(other) && OtherReadWidens(other, command, declaration, context.Compilation, backed))
                ? "another read uses the same source without the same stream dimensions" : null;
        if (reason is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0038_StreamReadGuardWidens, parameter.Locations.FirstOrDefault(), command.Name, parameter.Type.Name, reason));
        }
    }

    static string? Mismatch(INamedTypeSymbol command, IParameterSymbol parameter, INamedTypeSymbol source, string stream, Compilation compilation)
    {
        if (CommandAttributeSymbols.Find(parameter, "Cratis.Arc.Commands.LookUpByAttribute") is not null)
        {
            return "LookUpBy cannot redirect a stream belonging to the command's own source";
        }
        var route = EventSourceDefinitionSymbols.Find(command, EventSourceDefinitionSymbols.DeclarationAttribute);
        var matches = route is not null
            ? EventSourceDefinitionSymbols.NameOf((INamedTypeSymbol)route.AttributeClass!.TypeArguments[0]) == EventSourceDefinitionSymbols.NameOf(source) &&
                (route.ConstructorArguments.FirstOrDefault().Value as string) == stream
            : StringRoute(command, "Cratis.Chronicle.Events.EventSourceTypeAttribute") == EventSourceDefinitionSymbols.NameOf(source) &&
                StringRoute(command, "Cratis.Chronicle.Events.EventStreamTypeAttribute") == stream;
        if (!matches) return "its declared route does not select this source and stream";
        var id = EventSourceDefinitionSymbols.Find(command, "Cratis.Chronicle.Events.EventStreamIdAttribute");
        var value = id?.ConstructorArguments.FirstOrDefault().Value as string;
        return (!string.IsNullOrWhiteSpace(value) && value != "Default") ||
            ChronicleStateSymbols.Implements(command, "Cratis.Chronicle.Events.ICanProvideEventStreamId", compilation)
                ? null : "it declares no stream id";
    }

    static string? StringRoute(INamedTypeSymbol command, string attribute) =>
        EventSourceDefinitionSymbols.Find(command, attribute)?.ConstructorArguments.FirstOrDefault().Value as string;

    static bool OtherReadWidens(IParameterSymbol other, INamedTypeSymbol command, AttributeData declaration, Compilation compilation, Lazy<ImmutableHashSet<INamedTypeSymbol>> backed)
    {
        if (PerStreamReadModelSymbols.Declaration(other.Type) is { } otherStream)
        {
            return PerStreamReadModelSymbols.Stream(otherStream) != PerStreamReadModelSymbols.Stream(declaration) ||
                !SymbolEqualityComparer.Default.Equals(PerStreamReadModelSymbols.Source(otherStream), PerStreamReadModelSymbols.Source(declaration));
        }
        if (!ChronicleStateSymbols.IsBacked(other.Type, compilation, backed)) return false;
        var key = ReadProtectionSymbols.Lookup(command, other, compilation);
        return key is not null && ChronicleStateSymbols.IsKey(key, compilation);
    }

    static bool WidensByDeclaration(INamedTypeSymbol source, string stream)
    {
        var streamDeclaration = source.GetAttributes().FirstOrDefault(attribute =>
            EventSourceDefinitionSymbols.FullName(attribute.AttributeClass) == "Cratis.Chronicle.EventSources.EventStreamAttribute" &&
            (attribute.ConstructorArguments.FirstOrDefault().Value as string) == stream);
        static int Dimensions(AttributeData? attribute) => attribute?.NamedArguments.FirstOrDefault(pair => pair.Key == "Concurrency").Value.Value is int value ? value : 0;
        var dimensions = Dimensions(streamDeclaration);
        if (dimensions == 0) dimensions = Dimensions(EventSourceDefinitionSymbols.Find(source, "Cratis.Chronicle.EventSources.EventSourceAttribute"));
        return dimensions != 0 && (dimensions & 1) != 0 && (dimensions & 12) != 12;
    }
}
