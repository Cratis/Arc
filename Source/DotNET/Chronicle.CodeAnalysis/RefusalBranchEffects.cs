// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Proves invalid refusal productions from literal constructions; opaque values stay runtime-validated.
/// </summary>
/// <param name="compilation">The compilation identifying framework types.</param>
sealed class RefusalBranchEffects(Compilation compilation)
{
    const string Envelope = "Cratis.Chronicle.EventSequences.EventForEventSourceId";

    /// <summary>
    /// Determines whether a returned value contains a provably rejected production.
    /// </summary>
    /// <param name="value">The callback's returned value.</param>
    /// <returns>Whether the runtime rejects a statically visible branch of the value.</returns>
    internal bool IsInvalid(IOperation value) => IsInvalid(value, false);

    IOperation Unwrap(IOperation value) => value switch
    {
        IConversionOperation { OperatorMethod: null } conversion => Unwrap(conversion.Operand),
        IParenthesizedOperation parentheses => Unwrap(parentheses.Operand),
        _ => value
    };

    bool IsInvalid(IOperation value, bool member)
    {
        value = Unwrap(value);
        if (value.ConstantValue is { HasValue: true, Value: null })
        {
            return member;
        }

        return value switch
        {
            IConditionalOperation conditional => InvalidConditional(conditional, member),
            ISwitchExpressionOperation selection => selection.Arms.Any(arm => IsInvalid(arm.Value, member)),
            _ => InvalidProduction(value, member)
        };
    }

    bool InvalidConditional(IConditionalOperation conditional, bool member) =>
        IsInvalid(conditional.WhenTrue, member) ||
        (conditional.WhenFalse is { } otherwise && IsInvalid(otherwise, member));

    bool InvalidProduction(IOperation value, bool member) => value switch
    {
        IObjectCreationOperation creation => InvalidCreation(creation, member),
        IArrayCreationOperation array => InvalidArray(array, member),
        ICollectionExpressionOperation collection => InvalidCollection(collection, member),
        _ => InvalidScalar(value)
    };

    bool InvalidScalar(IOperation value) => value switch
    {
        IInvocationOperation invocation => IsEmptyArray(invocation),
        IAnonymousObjectCreationOperation or ILiteralOperation => true,
        _ => false
    };

    bool InvalidCreation(IObjectCreationOperation creation, bool member)
    {
        if (creation.Type is not { } type || type.TypeKind == TypeKind.Error)
        {
            return false;
        }

        if (IsCommand(type))
        {
            return true;
        }

        if (IsType(type, Envelope))
        {
            var payload = creation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 1)?.Value;

            return payload is not null && InvalidEnvelopePayload(payload);
        }

        if (RawGuidResponseAnalysis.HasEventTypeAttribute(type, compilation))
        {
            return false;
        }

        return IsLiteralCollection(type) ? InvalidLiteralCollection(creation, member) : !IsObjectSequence(type);
    }

    bool InvalidLiteralCollection(IObjectCreationOperation creation, bool member)
    {
        if (!IsObjectSequence(creation.Type))
        {
            return true;
        }

        var elements = creation.Initializer?.Initializers.OfType<IInvocationOperation>()
            .SelectMany(add => add.Arguments.Select(argument => argument.Value)).ToArray() ?? [];

        return member || elements.Any(element => IsInvalid(element, true)) ||
            (creation.Arguments.Length == 0 && elements.Length == 0);
    }

    bool InvalidEnvelopePayload(IOperation payload)
    {
        payload = Unwrap(payload);

        return payload is IConditionalOperation conditional ? InvalidEnvelopeConditional(conditional) : InvalidEnvelopeEvent(payload);
    }

    bool InvalidEnvelopeConditional(IConditionalOperation conditional) =>
        InvalidEnvelopePayload(conditional.WhenTrue) ||
        (conditional.WhenFalse is { } otherwise && InvalidEnvelopePayload(otherwise));

    /// <summary>
    /// Checks that an envelope carries one event, never another envelope or an event batch.
    /// </summary>
    /// <param name="payload">The unwrapped payload.</param>
    /// <returns>Whether the payload is statically invalid.</returns>
    bool InvalidEnvelopeEvent(IOperation payload) =>
        IsType(payload.Type, Envelope) || IsObjectSequence(payload.Type) || IsInvalid(payload, true);

    bool InvalidArray(IArrayCreationOperation array, bool member)
    {
        if (!IsObjectSequence(array.Type))
        {
            return true;
        }

        return member || (array.Initializer is { } initializer
            ? initializer.ElementValues.Length == 0 || initializer.ElementValues.Any(element => IsInvalid(element, true))
            : array.DimensionSizes.Any(size => size.ConstantValue is { HasValue: true, Value: 0 }));
    }

    bool InvalidCollection(ICollectionExpressionOperation collection, bool member)
    {
        if (collection.ConstructMethod is { MethodKind: MethodKind.Ordinary })
        {
            return false;
        }

        var elements = collection.Elements.Where(element => element.Syntax is not SpreadElementSyntax).ToArray();

        return !IsObjectSequence(collection.Type) || member || collection.Elements.Length == 0 || elements.Any(element => IsInvalid(element, true));
    }

    bool IsCommand(ITypeSymbol type) => type is INamedTypeSymbol named &&
        RawGuidResponseAnalysis.HandleMethods(named).Any() && HasCommandAttribute(type);

    bool HasCommandAttribute(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (RawGuidResponseAnalysis.HasAttribute(current, "Cratis.Arc.Commands.ModelBound.CommandAttribute", compilation))
            {
                return true;
            }
        }

        return false;
    }

    bool IsEmptyArray(IInvocationOperation invocation) =>
        invocation.TargetMethod.Name == "Empty" && IsType(invocation.TargetMethod.ContainingType, "System.Array");

    bool IsLiteralCollection(ITypeSymbol type) =>
        IsType(type, "System.Collections.Generic.List`1") || IsType(type, "System.Collections.Generic.HashSet`1");

    bool IsObjectSequence(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type is IArrayTypeSymbol array)
        {
            return array.Rank == 1 && MayBeReference(array.ElementType);
        }

        return IsObjectEnumerable(type) || type.AllInterfaces.Any(IsObjectEnumerable);
    }

    bool IsObjectEnumerable(ITypeSymbol type) =>
        type is INamedTypeSymbol named && IsType(named, "System.Collections.Generic.IEnumerable`1") &&
        MayBeReference(named.TypeArguments[0]);

    bool MayBeReference(ITypeSymbol type) =>
        type.IsReferenceType || type is ITypeParameterSymbol { IsValueType: false };

    bool IsType(ITypeSymbol? type, string name) => type is not null &&
        SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, compilation.GetTypeByMetadataName(name));
}
