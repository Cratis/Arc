// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that reports a command property whose name reads like a secret and which is not marked
/// <c>[NotAudited]</c>.
/// </summary>
/// <remarks>
/// <para>
/// A command's property values are recorded on the causation of every event it appends, and the causation is written
/// into the event log and stays there for as long as the events do. A value that should never have been written
/// cannot be taken back out by editing code, which makes this the rare case where a name-based guess earns its
/// place: the cost of a false positive is one attribute, and the cost of a miss is a password in the log forever.
/// </para>
/// <para>
/// The guess is deliberately narrow. It fires on whole words a reader would recognize as a secret, not on any
/// substring, so <c>PasswordPolicyId</c> is reported - it does contain the word - while <c>Passenger</c> is not.
/// Marking the property, the positional parameter, or the command itself silences it; so does marking the value as
/// personal data, since Chronicle already withholds that.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandSensitiveValueAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string NotAuditedAttributeName = "Cratis.Arc.Chronicle.Commands.NotAuditedAttribute";
    const string PiiAttributeName = "Cratis.Chronicle.Compliance.GDPR.PIIAttribute";

    /// <summary>
    /// The words that make a property name read as a secret.
    /// </summary>
    /// <remarks>
    /// Words, not fragments. Matching a fragment turns <c>Passenger</c> and <c>Subtotal</c> into findings, and an
    /// analyzer that cries wolf gets suppressed wholesale - taking the real findings with it.
    /// </remarks>
    static readonly ImmutableArray<string> _sensitiveWords =
    [
        "Password",
        "Passphrase",
        "Secret",
        "Token",
        "ApiKey",
        "AccessKey",
        "SecretKey",
        "PrivateKey",
        "Credential",
        "Credentials",
        "Pin",
        "Otp",
        "Cvv",
        "Cvc",
        "SecurityCode",
        "AuthorizationHeader"
    ];

    /// <summary>
    /// The types a secret is never held in, whatever the property is called.
    /// </summary>
    /// <remarks>
    /// A name is weak evidence on its own. <c>AccessTokenExpiresAt</c> contains the word "token" and holds a
    /// <see cref="DateTimeOffset"/> - it is a timestamp, and reporting it teaches people that the rule guesses badly.
    /// This is an exclusion list rather than an "only strings" rule on purpose: skipping a date is provably safe,
    /// while assuming only a string can hold a secret is not - a key can be a <c>byte[]</c>.
    /// </remarks>
    static readonly ImmutableHashSet<SpecialType> _typesThatHoldNoSecret =
    [
        SpecialType.System_Boolean,
        SpecialType.System_Byte,
        SpecialType.System_SByte,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_DateTime
    ];

    static readonly ImmutableArray<string> _typeNamesThatHoldNoSecret =
    [
        "System.DateTimeOffset",
        "System.DateOnly",
        "System.TimeOnly",
        "System.TimeSpan",
        "System.Guid"
    ];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0009_CommandSensitiveValueShouldNotBeAudited];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    /// <summary>
    /// Splits a member name into the words it is written from, so a word can be recognized without matching a
    /// fragment of a longer one.
    /// </summary>
    /// <param name="name">The name to split.</param>
    /// <returns>The words the name is composed of.</returns>
    internal static IEnumerable<string> WordsIn(string name)
    {
        var start = 0;

        for (var index = 1; index <= name.Length; index++)
        {
            var atEnd = index == name.Length;
            var startsNewWord = !atEnd && char.IsUpper(name[index]) && !char.IsUpper(name[index - 1]);
            var startsAcronymTail = !atEnd && index + 1 < name.Length && char.IsUpper(name[index]) && char.IsUpper(name[index - 1]) && char.IsLower(name[index + 1]);

            if (!atEnd && !startsNewWord && !startsAcronymTail)
            {
                continue;
            }

            if (index > start)
            {
                yield return name.Substring(start, index - start);
            }

            start = index;
        }
    }

    /// <summary>
    /// Determines whether a member name reads as carrying a secret.
    /// </summary>
    /// <param name="name">The name to judge.</param>
    /// <returns>True when the name contains a word that reads as a secret, false otherwise.</returns>
    internal static bool ReadsAsSensitive(string name)
    {
        var words = WordsIn(name).ToArray();

        // A single word ("ApiKey" splits into "Api" and "Key") is matched on its own, and adjacent pairs cover the
        // compound words that only read as a secret together - "Api" + "Key", not every "Key".
        for (var index = 0; index < words.Length; index++)
        {
            if (_sensitiveWords.Contains(words[index], StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (index + 1 < words.Length && _sensitiveWords.Contains(words[index] + words[index + 1], StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Without Chronicle there is no causation chain, so no command value is recorded anywhere and there is
        // nothing to warn about.
        if (context.Compilation.GetTypeByMetadataName(NotAuditedAttributeName) is null)
        {
            return;
        }

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var command = (INamedTypeSymbol)context.Symbol;

        if (!HasAttribute(command, CommandAttributeName) || IsExcluded(command))
        {
            return;
        }

        foreach (var property in command.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (!ReadsAsSensitive(property.Name) ||
                HoldsNoSecret(property.Type) ||
                IsExcluded(property) ||
                IsExcluded(property.Type) ||
                IsExcludedThroughParameter(command, property))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0009_CommandSensitiveValueShouldNotBeAudited,
                property.Locations[0],
                command.Name,
                property.Name));
        }
    }

    /// <summary>
    /// Determines whether a type is one a secret is never held in, so a name that reads as a secret can be
    /// disregarded.
    /// </summary>
    /// <param name="type">The type of the property.</param>
    /// <returns>True when the type cannot hold a secret, false otherwise.</returns>
    /// <remarks>
    /// A concept is judged by the value it wraps, since that is what would be recorded: a
    /// <c>ConceptAs&lt;DateTimeOffset&gt;</c> called <c>TokenExpiry</c> is as much a timestamp as the bare type is.
    /// </remarks>
    static bool HoldsNoSecret(ITypeSymbol type)
    {
        var underlying = UnderlyingTypeOf(type);

        return _typesThatHoldNoSecret.Contains(underlying.SpecialType) ||
               underlying.TypeKind == TypeKind.Enum ||
               _typeNamesThatHoldNoSecret.Contains(underlying.ToDisplayString());
    }

    static ITypeSymbol UnderlyingTypeOf(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } nullable && nullable.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            type = nullable.TypeArguments[0];
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && string.Equals(current.Name, "ConceptAs", StringComparison.Ordinal))
            {
                return current.TypeArguments[0];
            }
        }

        return type;
    }

    /// <summary>
    /// Determines whether a symbol carries a marking that keeps its value off the causation chain.
    /// </summary>
    /// <param name="symbol">The property, parameter, type, or command to check.</param>
    /// <returns>True when the symbol is marked, false otherwise.</returns>
    /// <remarks>
    /// Applied to a type this is how a concept carries its own marking: mark <c>ApiKey</c> once and every command
    /// that takes one is covered. The runtime withholds on exactly these markings - including the property's type -
    /// so reporting one it already honors would be reporting correct code.
    /// </remarks>
    static bool IsExcluded(ISymbol symbol) =>
        HasAttribute(symbol, NotAuditedAttributeName) ||
        HasAttribute(symbol, PiiAttributeName);

    /// <summary>
    /// Determines whether the positional record parameter a property came from carries a marking that silences the
    /// diagnostic.
    /// </summary>
    /// <param name="command">The command the property belongs to.</param>
    /// <param name="property">The property to check the originating parameter of.</param>
    /// <returns>True when the parameter is marked, false otherwise.</returns>
    /// <remarks>
    /// An attribute written on a positional parameter lands on the parameter rather than on the property it
    /// generates, unless it is spelled <c>[property: ...]</c>, and both are what someone means.
    /// </remarks>
    static bool IsExcludedThroughParameter(INamedTypeSymbol command, IPropertySymbol property)
    {
        var parameter = command.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault()?
            .Parameters
            .FirstOrDefault(_ => string.Equals(_.Name, property.Name, StringComparison.OrdinalIgnoreCase));

        return parameter is not null && IsExcluded(parameter);
    }

    static bool HasAttribute(ISymbol symbol, string fullyQualifiedName) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == fullyQualifiedName);
}
