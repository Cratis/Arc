// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports a <c>concurrency: true</c> flag on a stream-metadata attribute of a command whose handler returns exact concurrency scopes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandConcurrencyAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The name of the attribute argument that includes the attribute in the concurrency scope.
    /// </summary>
    internal const string ConcurrencyArgumentName = "concurrency";

    const string ScopesTypeName = "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes";
    const string EventStreamIdAttributeName = "Cratis.Chronicle.Events.EventStreamIdAttribute";

    static readonly string[] _attributeNames =
    [
        "Cratis.Chronicle.Events.EventSourceTypeAttribute",
        "Cratis.Chronicle.Events.EventStreamTypeAttribute",
        EventStreamIdAttributeName
    ];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARCCHR0016_ConcurrencyAttributeIgnoredByExactScopes];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeCommand, SymbolKind.NamedType);
    }

    /// <summary>
    /// Finds the argument that sets the concurrency flag of an attribute application.
    /// </summary>
    /// <param name="attribute">The attribute syntax.</param>
    /// <returns>The argument, or null when the flag is not passed.</returns>
    internal static AttributeArgumentSyntax? FindConcurrencyArgument(AttributeSyntax attribute)
    {
        var arguments = attribute.ArgumentList?.Arguments;
        if (arguments is null)
        {
            return null;
        }

        for (var index = 0; index < arguments.Value.Count; index++)
        {
            var argument = arguments.Value[index];
            if (argument.NameColon is { } name)
            {
                if (name.Name.Identifier.ValueText == ConcurrencyArgumentName)
                {
                    return argument;
                }
            }
            else if (argument.NameEquals is null && index == 1)
            {
                return argument;
            }
        }

        return null;
    }

    static void AnalyzeCommand(SymbolAnalysisContext context)
    {
        var command = (INamedTypeSymbol)context.Symbol;
        if (command.TypeKind != TypeKind.Class ||
            !RawGuidResponseAnalysis.HasAttribute(command, "Cratis.Arc.Commands.ModelBound.CommandAttribute", context.Compilation) ||
            !RawGuidResponseAnalysis.HandleMethods(command).Any(method => ReturnsScopes(method.ReturnType, context.Compilation)))
        {
            return;
        }

        foreach (var attribute in command.GetAttributes())
        {
            if (!_attributeNames.Any(name => RawGuidResponseAnalysis.IsType(attribute.AttributeClass, name, context.Compilation)) ||
                attribute.ConstructorArguments.Length < 2 ||
                attribute.ConstructorArguments[1].Value is not true ||
                attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is not AttributeSyntax syntax ||
                FindConcurrencyArgument(syntax) is not { } argument)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0016_ConcurrencyAttributeIgnoredByExactScopes,
                argument.GetLocation(),
                command.Name,
                attribute.AttributeClass!.Name.Substring(0, attribute.AttributeClass.Name.Length - "Attribute".Length)));
        }
    }

    static bool ReturnsScopes(ITypeSymbol returnType, Compilation compilation) =>
        RawGuidResponseAnalysis.UnionBranches(RawGuidResponseAnalysis.UnwrapAwaitable(returnType, compilation), compilation)
            .Any(branch => IsScopes(branch, compilation) ||
                (branch is INamedTypeSymbol { IsTupleType: true } tuple &&
                 tuple.TupleElements.SelectMany(element => RawGuidResponseAnalysis.UnionBranches(element.Type, compilation))
                     .Any(element => IsScopes(element, compilation))));

    static bool IsScopes(ITypeSymbol type, Compilation compilation) =>
        RawGuidResponseAnalysis.IsType(type, ScopesTypeName, compilation);
}
