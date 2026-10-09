// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Validates <c>[EventStreamId]</c> templates and the way a command declares its event stream id.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandEventStreamIdAnalyzer : DiagnosticAnalyzer
{
    const string AttributeName = "Cratis.Chronicle.Events.EventStreamIdAttribute";
    const string EventSourceIdName = "Cratis.Chronicle.Events.EventSourceId";
    const string ReactorName = "Cratis.Chronicle.Reactors.IReactor";
    const string ConceptName = "Cratis.Concepts.ConceptAs`1";

    static readonly string[] _convertibleStructs = ["System.Guid", "System.DateOnly", "System.DateTimeOffset", "System.TimeOnly", "System.TimeSpan"];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0017_InvalidEventStreamIdTemplate,
        DiagnosticDescriptors.ARCCHR0018_EventStreamIdTemplateOnlyResolvedForCommands
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeCommand, SymbolKind.NamedType);
    }

    static void AnalyzeCommand(SymbolAnalysisContext context)
    {
        var command = (INamedTypeSymbol)context.Symbol;
        if (command.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!RawGuidResponseAnalysis.HasAttribute(command, "Cratis.Arc.Commands.ModelBound.CommandAttribute", context.Compilation))
        {
            AnalyzeReactor(context, command);
            return;
        }

        var attribute = command.GetAttributes().FirstOrDefault(_ => RawGuidResponseAnalysis.IsType(_.AttributeClass, AttributeName, context.Compilation));
        if (attribute is null ||
            attribute.ConstructorArguments.Length == 0 ||
            attribute.ConstructorArguments[0].Value is not string value ||
            attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is not AttributeSyntax syntax)
        {
            return;
        }

        var location = syntax.ArgumentList?.Arguments.FirstOrDefault()?.GetLocation() ?? syntax.GetLocation();
        foreach (var problem in Problems(value, command, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0017_InvalidEventStreamIdTemplate,
                location,
                command.Name,
                value,
                problem));
        }
    }

    static void AnalyzeReactor(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        var attribute = type.GetAttributes().FirstOrDefault(_ => RawGuidResponseAnalysis.IsType(_.AttributeClass, AttributeName, context.Compilation));
        if (attribute is null ||
            attribute.ConstructorArguments.Length == 0 ||
            attribute.ConstructorArguments[0].Value is not string value ||
            !HasPlaceholder(value) ||
            !type.AllInterfaces.Any(_ => RawGuidResponseAnalysis.IsType(_, ReactorName, context.Compilation)) ||
            attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is not AttributeSyntax syntax)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0018_EventStreamIdTemplateOnlyResolvedForCommands,
            syntax.ArgumentList?.Arguments.FirstOrDefault()?.GetLocation() ?? syntax.GetLocation(),
            type.Name,
            value));
    }

    static bool HasPlaceholder(string template)
    {
        for (var index = 0; index < template.Length; index++)
        {
            if (template[index] == '{' && index + 1 < template.Length && template[index + 1] == '{')
            {
                index++;
            }
            else if (template[index] == '{' && template.IndexOf('}', index + 1) > index + 1)
            {
                return true;
            }
        }

        return false;
    }

    static IEnumerable<string> Problems(string template, INamedTypeSymbol command, Compilation compilation)
    {
        var index = 0;
        while (index < template.Length)
        {
            var character = template[index];
            if (character == '{')
            {
                if (index + 1 < template.Length && template[index + 1] == '{')
                {
                    index += 2;
                    continue;
                }

                var end = template.IndexOf('}', index + 1);
                var nextOpen = template.IndexOf('{', index + 1);
                if (end < 0 || (nextOpen >= 0 && nextOpen < end))
                {
                    yield return $"the brace at position {index} is not closed (write {{{{ for a literal brace)";
                    yield break;
                }

                var name = template.Substring(index + 1, end - index - 1);
                if (name.Length == 0)
                {
                    yield return "a placeholder has no property name";
                }
                else
                {
                    var problem = PropertyProblem(name, command, compilation);
                    if (problem is not null)
                    {
                        yield return problem;
                    }
                }

                index = end + 1;
            }
            else if (character == '}')
            {
                if (index + 1 < template.Length && template[index + 1] == '}')
                {
                    index += 2;
                    continue;
                }

                yield return $"the brace at position {index} was never opened (write }}}} for a literal brace)";
                yield break;
            }
            else
            {
                index++;
            }
        }
    }

    static string? PropertyProblem(string name, INamedTypeSymbol command, Compilation compilation)
    {
        var property = RawGuidResponseAnalysis.PublicProperties(command).FirstOrDefault(_ => !_.IsStatic && !_.IsIndexer && _.Name == name);
        if (property is null)
        {
            return $"'{name}' is not a public instance property of the command";
        }

        return IsStringConvertible(property.Type, compilation)
            ? null
            : $"property '{name}' of type '{property.Type.ToDisplayString()}' does not convert to a string part";
    }

    static bool IsStringConvertible(ITypeSymbol type, Compilation compilation)
    {
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol nullable)
        {
            type = nullable.TypeArguments[0];
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
        }

        if (_convertibleStructs
            .Any(_ => RawGuidResponseAnalysis.IsType(type, _, compilation)))
        {
            return true;
        }

        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (RawGuidResponseAnalysis.IsType(current, EventSourceIdName, compilation))
            {
                return true;
            }

            if (RawGuidResponseAnalysis.IsType(current, ConceptName, compilation))
            {
                return IsStringConvertible(current.TypeArguments[0], compilation);
            }
        }

        return false;
    }
}
