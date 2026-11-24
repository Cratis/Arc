// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the lines of an availability block back from the code that states them with operators.
/// </summary>
/// <remarks>
/// <c>For&lt;Plus&gt;()</c>, <c>Not&lt;Incident&gt;()</c>, <c>With&lt;Other&gt;()</c> and <c>When.On&lt;Vessel&gt;(_ =&gt; _.Property).Is(value)</c> are the
/// terms, and <c>&amp;</c> and <c>|</c> combine them. Anything else is not something the language can say.
/// </remarks>
public static class AvailabilityTerms
{
    const string WhenType = "Cratis.Arc.Availability.AvailabilityWhen";
    const string OperandType = "Cratis.Arc.Availability.ContextOperand";

    /// <summary>
    /// Gets the name a condition class is given in the model: its type name in camelCase, without a trailing <c>Condition</c>.
    /// </summary>
    /// <param name="conditionType">The name of the condition class.</param>
    /// <returns>The name.</returns>
    public static string OpaqueName(string conditionType)
    {
        var name = conditionType.Length > "Condition".Length && conditionType.EndsWith("Condition", StringComparison.Ordinal)
            ? conditionType[..^"Condition".Length]
            : conditionType;

        return CamelCase(name);
    }

    /// <summary>
    /// Converts a name to camelCase.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The name with its first character in lower case.</returns>
    public static string CamelCase(string name) => name.Length == 0 || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>
    /// Reads the lines a block defines.
    /// </summary>
    /// <param name="block">The type of the block.</param>
    /// <param name="compilation">The compilation the block is in.</param>
    /// <returns>The lines, or the reason they cannot be read.</returns>
    public static (IReadOnlyList<AvailabilityRequirementModel>? Lines, string Reason) LinesOf(INamedTypeSymbol block, Compilation compilation)
    {
        var lines = block.GetMembers("Lines").OfType<IPropertySymbol>().FirstOrDefault(_ => _.IsOverride);
        var syntax = lines?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax is null)
        {
            return (null, "the block does not override Lines in source");
        }

        var expression = syntax switch
        {
            Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax { ExpressionBody: { } body } => body.Expression,
            Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property => property.AccessorList?.Accessors
                .FirstOrDefault()?.ExpressionBody?.Expression ?? property.AccessorList?.Accessors.FirstOrDefault()?.Body?.Statements
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ReturnStatementSyntax>().FirstOrDefault()?.Expression,
            _ => null
        };

        if (expression is null || compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(expression) is not { } operation)
        {
            return (null, "Lines is not written as one expression");
        }

        var elements = Elements(operation);
        if (elements is null)
        {
            return (null, "Lines is not written as a collection of terms");
        }

        var result = new List<AvailabilityRequirementModel>();
        foreach (var element in elements)
        {
            var (requirement, reason) = Term(element);
            if (requirement is null)
            {
                return (null, reason);
            }

            result.Add(requirement);
        }

        return (result, string.Empty);
    }

    static IEnumerable<IOperation>? Elements(IOperation operation)
    {
        operation = Unwrap(operation);

        return operation switch
        {
            ICollectionExpressionOperation collection => collection.Elements,
            IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues,
            _ => null
        };
    }

    static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    static (AvailabilityRequirementModel? Requirement, string Reason) Term(IOperation operation)
    {
        operation = Unwrap(operation);

        if (operation is IBinaryOperation { OperatorMethod: { } op } binary && (string.Equals(op.Name, "op_BitwiseAnd", StringComparison.Ordinal) || string.Equals(op.Name, "op_BitwiseOr", StringComparison.Ordinal)))
        {
            var isAnd = op.Name == "op_BitwiseAnd";
            var (left, leftReason) = Term(binary.LeftOperand);
            var (right, rightReason) = Term(binary.RightOperand);
            if (left is null || right is null)
            {
                return (null, left is null ? leftReason : rightReason);
            }

            if (op.ContainingType.Name == "ConditionTerm" && left is ConditionRequirementModel leftCondition && right is ConditionRequirementModel rightCondition)
            {
                return (new ConditionRequirementModel(new LogicalConditionModel(leftCondition.Condition, isAnd, rightCondition.Condition)), string.Empty);
            }

            return (new LogicalRequirementModel(left, isAnd, right), string.Empty);
        }

        if (operation is not IInvocationOperation invocation)
        {
            return (null, "a term is not For<>(), Not<>(), With<>() or a When condition");
        }

        var method = invocation.TargetMethod;
        var containing = method.ContainingType;
        if (containing.Is(WhenType) && method.Name == "Condition" && method.TypeArguments.Length == 1)
        {
            return (new ConditionRequirementModel(new OpaqueConditionModel(OpaqueName(method.TypeArguments[0].Name))), string.Empty);
        }

        if (containing.Is(OperandType))
        {
            return Comparison(invocation);
        }

        if (method.TypeArguments.Length == 1 && containing.Is(WellKnownTypeNames.AvailabilityBlock))
        {
            var name = method.TypeArguments[0].Name;
            switch (method.Name)
            {
                case "For":
                    return (new AudienceRequirementModel(name), string.Empty);
                case "Not":
                    return (new AudienceRequirementModel(name, true), string.Empty);
                case "With":
                    return (new ReferenceRequirementModel(name), string.Empty);
            }
        }

        return (null, $"{method.Name} is not a term the language can state");
    }

    static (AvailabilityRequirementModel? Requirement, string Reason) Comparison(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        if (Unwrap(invocation.Instance ?? invocation) is not IInvocationOperation on || !on.TargetMethod.ContainingType.Is(WhenType) || on.TargetMethod.Name != "On")
        {
            return (null, "a comparison does not start with When.On<ReadModel>(...)");
        }

        var path = PathOf(on.Arguments.FirstOrDefault()?.Value);
        if (path is null)
        {
            return (null, "a comparison reads something other than a property, or a chain of properties, of the read model");
        }

        var operands = new List<object>();
        foreach (var argument in invocation.Arguments)
        {
            foreach (var value in Values(argument.Value))
            {
                var operand = Operand(value);
                if (operand is null)
                {
                    return (null, $"the operand of {method.Name} is not a constant");
                }

                operands.Add(operand);
            }
        }

        var read = $"{CamelCase(on.TargetMethod.TypeArguments[0].Name)}.{path}";

        return (new ConditionRequirementModel(new ComparisonConditionModel(read, method.Name, operands)), string.Empty);
    }

    static System.Collections.Immutable.ImmutableArray<IOperation> Values(IOperation? operation)
    {
        if (operation is null)
        {
            return [];
        }

        var unwrapped = Unwrap(operation);

        return unwrapped is IArrayCreationOperation { Initializer: { } initializer } ? initializer.ElementValues : [operation];
    }

    static object? Operand(IOperation operation)
    {
        var boxed = Unwrap(operation);
        var type = boxed.Type;
        if (!boxed.ConstantValue.HasValue || boxed.ConstantValue.Value is null)
        {
            return null;
        }

        if (type is { TypeKind: TypeKind.Enum })
        {
            return type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(_ => _.HasConstantValue && Equals(_.ConstantValue, boxed.ConstantValue.Value))?.Name;
        }

        return boxed.ConstantValue.Value;
    }

    static string? PathOf(IOperation? argument)
    {
        if (argument is null)
        {
            return null;
        }

        var current = Unwrap(argument);
        if (current is IDelegateCreationOperation delegateCreation)
        {
            current = delegateCreation.Target;
        }

        if (current is not IAnonymousFunctionOperation function)
        {
            return null;
        }

        var returned = function.Body.Operations.OfType<IReturnOperation>().FirstOrDefault()?.ReturnedValue;
        var segments = new List<string>();
        var step = returned is null ? null : Unwrap(returned);
        while (step is IPropertyReferenceOperation or IFieldReferenceOperation)
        {
            if (step is IPropertyReferenceOperation property)
            {
                segments.Insert(0, CamelCase(property.Property.Name));
                step = property.Instance is null ? null : Unwrap(property.Instance);
            }
            else
            {
                var field = (IFieldReferenceOperation)step;
                segments.Insert(0, CamelCase(field.Field.Name));
                step = field.Instance is null ? null : Unwrap(field.Instance);
            }
        }

        return step is IParameterReferenceOperation && segments.Count > 0 ? string.Join('.', segments) : null;
    }
}
