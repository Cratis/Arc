// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Reads keyed command dependencies and simple provisioning rejection guards.</summary>
/// <param name="models">The source models.</param>
/// <param name="types">The type registry.</param>
/// <param name="diagnostics">The diagnostic sink.</param>
/// <param name="enabled">Whether authoring reads are enabled.</param>
public class CommandReadsReader(SemanticModels models, TypeRegistry types, ScreenplayDiagnostics diagnostics, bool enabled)
{
    readonly ReadConditions _conditions = new(diagnostics);
    readonly ScreenplayNaming _naming = new();

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
        var scope = new ReadScope(command, identifier, CommandAttributeSymbols.ResolveCommand(command).ReadProtection, sources, location);
        var methods = command.GetMembers().OfType<IMethodSymbol>().Where(method => !method.IsStatic && (method.Name == "Provide" || method.Name == "Handle")).ToArray();
        var providers = methods.Where(method => method.Name == "Provide").ToArray();
        var parameters = Parameters(methods, providers, scope);
        if (parameters.Length == 0 && !enabled)
        {
            return ([], []);
        }

        var guards = methods.SelectMany(method => ProtectedReadGuards.Leading(method, models)).ToArray();
        var accepted = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var reads = ReadDependencies(scope, parameters, guards, accepted);
        var requirements = new List<CommandRequirementModel>();
        var provisioned = providers.Select(provide => new ReadProvisions(models, _conditions, diagnostics).Read(provide, scope)).ToArray();
        foreach (var provision in provisioned.OfType<ProvisionRead>())
        {
            requirements.AddRange(provision.Requirements);
            CarryProvided(provision, methods, scope);
        }

        requirements.AddRange(HandleRequirements(methods, accepted, scope));
        ProvisionStatesReadsOnly = providers.Length > 0 && provisioned.All(provision => provision is not null) && reads.Exists(read => read.Protected);

        return (reads, requirements);
    }

    static bool IsProtected(IParameterSymbol parameter, ReadScope scope) => scope.Protection == CommandReadKeys.ProtectionOn &&
        !CommandAttributeSymbols.ResolveRead(ReadType(parameter.Type)!, (IMethodSymbol)parameter.ContainingSymbol, parameter).Advisory;

    static string? Conflict(ReadScope scope, IParameterSymbol parameter, string? key, List<CommandReadModel> reads)
    {
        var alias = parameter.Name;
        var type = ReadType(parameter.Type)!;
        if (scope.Command.DeclaredProperties().Any(property => string.Equals(property.Name, alias, StringComparison.OrdinalIgnoreCase)) || IsGrammarKeyword(alias))
        {
            return $"the read alias '{alias}' conflicts with command input or a grammar keyword";
        }

        return reads.Exists(read => read.Alias == alias && (read.Name != type.Name || read.Key != key))
            ? $"the read alias '{alias}' names different models or keys"
            : null;
    }

    static bool IsGrammarKeyword(string alias) => alias == "reads" || alias == "as" || alias == "by";

    static (string? Key, string? Failure) Key(ReadScope scope, IParameterSymbol parameter, INamedTypeSymbol type, bool absenceRejected) =>
        parameter.Type.NullableAnnotation == NullableAnnotation.Annotated && !absenceRejected
            ? (null, "the read is optional and no leading guard rejects its absence")
            : CommandReadKeys.Resolve(scope, parameter, type);

    static bool Provides(ITypeSymbol result, ITypeSymbol dependency) => SymbolEqualityComparer.Default.Equals(result, dependency) ||
        (result is INamedTypeSymbol named && named.TypeArguments.Any(argument => Provides(argument, dependency)));

    static INamedTypeSymbol? ReadType(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol named when named.Is("Cratis.Chronicle.ReadModels.DecisionRead`1") && named.TypeArguments is [INamedTypeSymbol read] => read,
        INamedTypeSymbol named when named.HasAttribute(WellKnownTypeNames.ReadModelAttribute) => named,
        _ => null
    };

    IParameterSymbol[] Parameters(IMethodSymbol[] methods, IMethodSymbol[] providers, ReadScope scope)
    {
        var parameters = methods.SelectMany(method => method.Parameters).Where(parameter => ReadType(parameter.Type) is not null &&
            (parameter.ContainingSymbol.Name != "Handle" || !providers.Any(provider => Provides(provider.ReturnType, parameter.Type)))).ToArray();
        if (enabled)
        {
            return parameters;
        }

        var protectedParameters = parameters.Where(parameter => IsProtected(parameter, scope)).ToArray();
        if (protectedParameters.Length < parameters.Length || (protectedParameters.Length == 0 && providers.Length > 0))
        {
            Report("Reads and provisioning requirements are authoring-only; enable ScreenplayOptions.AuthoringOnlyConstructs to describe readable dependencies and guards", scope);
        }

        return protectedParameters;
    }

    List<CommandReadModel> ReadDependencies(ReadScope scope, IParameterSymbol[] parameters, ReadGuard[] guards, HashSet<ISymbol> accepted)
    {
        var reads = new List<CommandReadModel>();
        foreach (var parameter in parameters)
        {
            var type = ReadType(parameter.Type)!;
            var isProtected = IsProtected(parameter, scope);
            var absence = isProtected ? guards.Where(guard => SymbolEqualityComparer.Default.Equals(ProtectedReadGuards.AbsenceOf(guard.Statement.Condition, guard.Model), parameter)).ToArray() : [];
            var (key, failure) = Key(scope, parameter, type, absence.Length > 0);
            failure ??= Conflict(scope, parameter, key, reads);
            if (failure is not null)
            {
                Refuse(isProtected, failure, scope);
                continue;
            }

            // Provide and Handle can share a dependency. A returned parameter is carried to Handle, not read twice.
            if (!reads.Exists(read => read.Alias == parameter.Name && read.Name == type.Name))
            {
                reads.Add(Describe(parameter, type, key!, isProtected));
            }

            if (isProtected)
            {
                accepted.Add(parameter);
                StateAbsence(parameter, absence, scope);
            }

            if (IsUnchanged(parameter, scope))
            {
                scope.Sources.Add(parameter, parameter.Name, parameter.Type.Is("Cratis.Chronicle.ReadModels.DecisionRead`1"));
            }
        }

        return reads;
    }

    CommandReadModel Describe(IParameterSymbol parameter, INamedTypeSymbol type, string key, bool isProtected) => new(type.Name, parameter.Name, key)
    {
        Namespace = type.ContainingNamespace.ToDisplayString(),
        FullName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(),
        Properties = new PropertyReader(types).Read(type).ToList(),
        Protected = isProtected,
        NamedByView = _naming.ToPropertyName(parameter.Name) == _naming.ToPropertyName(type.Name)
    };

    void CarryProvided(ProvisionRead provision, IMethodSymbol[] methods, ReadScope scope)
    {
        if (scope.Sources.ReadPath(provision.Provided, provision.Model) is not { } providedPath || providedPath.Contains('.', StringComparison.Ordinal))
        {
            return;
        }

        var carried = methods.Where(method => method.Name == "Handle").SelectMany(method => method.Parameters)
            .Where(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, provision.Model.GetTypeInfo(provision.Provided).Type));
        foreach (var parameter in carried.Where(parameter => IsUnchanged(parameter, scope)))
        {
            scope.Sources.Add(parameter, providedPath);
        }
    }

    void StateAbsence(IParameterSymbol parameter, IReadOnlyList<ReadGuard> absence, ReadScope scope)
    {
        foreach (var guard in absence)
        {
            scope.Sources.ConsumeGuard(guard.Statement);
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnprotectableCommandRead,
                $"Read '{parameter.Name}': the model rejects a missing instance itself, so the guard's message{(guard.Message is null ? string.Empty : $" '{guard.Message}'")} is not carried and the evaluator's absence message applies",
                scope.Location);
        }
    }

    IEnumerable<CommandRequirementModel> HandleRequirements(IMethodSymbol[] methods, HashSet<ISymbol> reads, ReadScope scope)
    {
        var guards = methods.Where(method => method.Name == "Handle").SelectMany(method => ProtectedReadGuards.Leading(method, models))
            .Where(guard => !scope.Sources.ConsumedGuards.Contains(guard.Statement) &&
                guard.Statement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(identifier => reads.Contains(guard.Model.GetSymbolInfo(identifier).Symbol!)));
        foreach (var guard in guards)
        {
            if (guard.Message is not null && _conditions.Condition(guard.Statement.Condition, guard.Model, scope, true) is { } condition)
            {
                scope.Sources.ConsumeGuard(guard.Statement);

                yield return new(condition, guard.Message);
            }
            else
            {
                diagnostics.Information(ScreenplayDiagnosticCodes.UnprotectableCommandRead, $"A guard over a protected read ({(guard.Message is null ? "its message is computed" : $"'{guard.Message}'")}) is not a requirement the model states; it was left in code", scope.Location);
            }
        }
    }

    void Refuse(bool isProtected, string failure, ReadScope scope)
    {
        if (isProtected)
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.UnprotectableCommandRead, $"A protected read was left in code, never emitted as an unguarded read: {failure}", scope.Location);
            return;
        }

        Report($"The read dependency was left in code: {failure}", scope);
    }

    bool IsUnchanged(IParameterSymbol parameter, ReadScope scope)
    {
        if (HandlerBodies.Of((IMethodSymbol)parameter.ContainingSymbol).ToArray() is not [var body] || models.For(body.SyntaxTree) is not { } model)
        {
            Report($"The body using read parameter '{parameter.Name}' is unavailable; dependent mappings and requirements were left in code", scope);
            return false;
        }

        if (!CommandAuthoringReader.IsUnchanged(parameter, body, model))
        {
            Report($"Read parameter '{parameter.Name}' is reassigned or passed by reference; dependent mappings and requirements were left in code", scope);
            return false;
        }

        return true;
    }

    void Report(string message, ReadScope scope) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandProvisioning, message, scope.Location);
}
