// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Recognizes command read-protection context without guessing service registrations or dataflow.
/// </summary>
internal static class ReadProtectionSymbols
{
    /// <summary>
    /// Finds the command associated with a handler, provider or validator, including nested callbacks.
    /// </summary>
    /// <param name="symbol">The enclosing declaration.</param>
    /// <returns>The command, or null outside a command dependency/body.</returns>
    internal static INamedTypeSymbol? CommandFor(ISymbol? symbol)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
        {
            symbol = symbol.ContainingSymbol;
        }
        if (symbol is not IMethodSymbol method)
        {
            return null;
        }
        var type = method.ContainingType;
        if ((method.Name == "Handle" || method.Name == "Provide") && IsCommand(type))
        {
            return type;
        }
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "CommandValidator" && current.Arity == 1 &&
                current.ContainingNamespace.ToDisplayString() == "Cratis.Arc.Commands" &&
                current.TypeArguments[0] is INamedTypeSymbol command)
            {
                return command;
            }
        }
        return null;
    }

    /// <summary>
    /// Recognizes model-bound command discovery, including inherited public Handle methods.
    /// </summary>
    /// <param name="type">The candidate command.</param>
    /// <returns>Whether the type declares a command.</returns>
    internal static bool IsCommand(INamedTypeSymbol type) =>
        CommandAttributeSymbols.Find(type, "Cratis.Arc.Commands.ModelBound.CommandAttribute") is not null ||
        RawGuidResponseAnalysis.HandleMethods(type).Any();

    /// <summary>
    /// Gets the compile-time rollout mode, with command and inherited opt-outs applied.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>Off=0, Measure=1 or On=2.</returns>
    internal static int Mode(INamedTypeSymbol command) => CommandAttributeSymbols.ResolveCommand(command).ReadProtection;

    /// <summary>
    /// Tests a framework type, its bases and interfaces by metadata name.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <param name="name">The metadata name.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether the candidate matches.</returns>
    internal static bool Is(ITypeSymbol type, string name, Compilation compilation)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (ChronicleStateSymbols.Implements(current, name, compilation))
            {
                return true;
            }
        }
        return type is ITypeParameterSymbol parameter && parameter.ConstraintTypes.Any(constraint => Is(constraint, name, compilation));
    }

    /// <summary>
    /// Identifies a parameter/type opt-out. Method-level Unprotected never opts out parameters.
    /// </summary>
    /// <param name="parameter">The injected parameter.</param>
    /// <returns>Whether the parameter is advisory.</returns>
    internal static bool Advisory(IParameterSymbol parameter) =>
        CommandAttributeSymbols.HasUnprotected(parameter) || CommandAttributeSymbols.HasUnprotected(parameter.Type);

    /// <summary>
    /// Finds the command property used by an automatic read, or null when it cannot be proven.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="parameter">The read parameter.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The known key property.</returns>
    internal static IPropertySymbol? Lookup(INamedTypeSymbol command, IParameterSymbol parameter, Compilation compilation)
    {
        var properties = RawGuidResponseAnalysis.PublicProperties(command).ToArray();
        var lookup = CommandAttributeSymbols.Find(parameter, AttributePrecedencePolicy.LookUpByName);
        if (lookup is not null)
        {
            return properties.FirstOrDefault(property => property.Name == (lookup.ConstructorArguments.FirstOrDefault().Value as string));
        }
        if (PerStreamReadModelSymbols.Declaration(parameter.Type) is not null)
        {
            return HasCommandKey();
        }
        IPropertySymbol? HasCommandKey() => ChronicleStateSymbols.HasProvider(command, compilation) ? null : properties.FirstOrDefault(property => ChronicleStateSymbols.IsKey(property, compilation));
        if (Mode(command) == 2 && parameter.Type is INamedTypeSymbol model && ChronicleStateSymbols.ModelKey(model, compilation) is { } key)
        {
            var matches = properties.Where(property => SymbolEqualityComparer.Default.Equals(property.Type, key.Type)).ToArray();
            if (matches.Length > 0)
            {
                return matches.Length == 1 ? matches[0] : null;
            }
        }
        return ChronicleStateSymbols.HasProvider(command, compilation) ? null : properties.FirstOrDefault(property => ChronicleStateSymbols.IsKey(property, compilation));
    }

    /// <summary>
    /// Checks whether an invocation is a guarded Chronicle acquisition or explicit read allowance.
    /// </summary>
    /// <param name="method">The invoked method.</param>
    /// <returns>Whether this API is refused/explicitly bypasses protection in an On body.</returns>
    internal static bool IsRawRead(IMethodSymbol method)
    {
        var type = method.ReducedFrom?.Parameters.FirstOrDefault()?.Type ?? method.ContainingType;
        var ns = method.ContainingNamespace.ToDisplayString();
        if (!ns.StartsWith("Cratis.Chronicle.", StringComparison.Ordinal))
        {
            return false;
        }
        return method.Name == "GetDetached" || method.Name == "AllowReads" ||
            ((type.Name == "IReadModels" || type.Name == "ReadModels" || type.Name == "IReadModel" || type.Name == "ReadModel") && method.Name.StartsWith("Get", StringComparison.Ordinal)) ||
            ((type.Name == "IEventSequence" || type.Name == "EventSequence" || type.Name == "IEventLog" || type.Name == "EventLog" || type.Name == "IEventSequenceQuery") &&
             (method.Name.StartsWith("Get", StringComparison.Ordinal) || method.Name == "Query" || method.Name == "Read"));
    }

    /// <summary>
    /// Identifies a method body with an advisory allowance, including callbacks inside it.
    /// </summary>
    /// <param name="symbol">The enclosing symbol.</param>
    /// <returns>Whether the method body is advisory.</returns>
    internal static bool AdvisoryBody(ISymbol? symbol)
    {
        for (var current = symbol; current is IMethodSymbol; current = current.ContainingSymbol)
        {
            if (CommandAttributeSymbols.HasUnprotected(current))
            {
                return true;
            }
        }
        return false;
    }
}
