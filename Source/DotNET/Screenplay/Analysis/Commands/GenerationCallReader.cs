// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Maps closed generation calls with literal arguments and passed-through context parameters.
/// </summary>
internal static class GenerationCallReader
{
    /// <summary>
    /// Reads a direct generation invocation without evaluating application code.
    /// </summary>
    /// <param name="expression">The expression being proved.</param>
    /// <param name="model">The semantic model resolving the call.</param>
    /// <returns>The recovered strategy, or null when the call cannot be stated.</returns>
    public static GenerationModel? Read(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax call || model.GetOperation(call) is not IInvocationOperation invocation)
        {
            return null;
        }
        if (!IsStatableInvocation(invocation))
        {
            return null;
        }

        return invocation.TargetMethod.ContainingType.FullMetadataName() switch
        {
            "Cratis.Concepts.GenerateValue" => Fundamentals(invocation.TargetMethod.Name, invocation),
            "System.Guid" => GuidCall(invocation),
            "System.Security.Cryptography.RandomNumberGenerator" when invocation.TargetMethod.Name == "GetHexString" => Hex(invocation),
            _ => null
        };
    }

    static bool IsStatableInvocation(IInvocationOperation invocation) => invocation.TargetMethod.IsStatic && invocation.Arguments.All(IsStatable);

    static GenerationModel? GuidCall(IInvocationOperation call) => call.TargetMethod.Name switch
    {
        "NewGuid" when call.Arguments.IsEmpty => new("uuid"),
        "CreateVersion7" when call.Arguments.IsEmpty => new("uuidv7"),
        _ => null
    };

    static bool IsStatable(IArgumentOperation argument)
    {
        if (argument.IsImplicit)
        {
            return true;
        }
        if (argument.Parameter?.Type.Is("Cratis.Concepts.GenerationContext") == true)
        {
            return argument.Value is IParameterReferenceOperation parameter && parameter.Parameter.Type.Is("Cratis.Concepts.GenerationContext");
        }

        return argument.Syntax is ArgumentSyntax { Expression: LiteralExpressionSyntax } || IsEncoding(argument);
    }

    static bool IsEncoding(IArgumentOperation argument) => argument.Value is IFieldReferenceOperation { Field.ContainingType: var type } && type.Is("Cratis.Concepts.ValueEncoding");

    static object? Value(IInvocationOperation call, string name) => call.Arguments.SingleOrDefault(argument => argument.Parameter?.Name == name)?.Value.ConstantValue.Value;

    static bool HasOnly(IInvocationOperation call, params string[] names) => call.Arguments.All(argument => names.Contains(argument.Parameter!.Name, StringComparer.Ordinal));

    static GenerationModel? Fundamentals(string name, IInvocationOperation call) => name switch
    {
        "Uuid" when call.Arguments.IsEmpty => new("uuid"),
        "UuidV7" when HasOnly(call, "context") => new("uuidv7"),
        "Ulid" when HasOnly(call, "context") => new("ulid"),
        "TypeId" when HasOnly(call, "prefix", "context") => TypeId(call),
        "NanoId" when HasOnly(call, "size", "alphabet") => NanoId(call),
        "Bytes" when HasOnly(call, "count", "encoding") => Bytes(call),
        _ => null
    };

    static GenerationModel? TypeId(IInvocationOperation call)
    {
        if (Value(call, "prefix") is not string prefix || !ValidPrefix(prefix))
        {
            return null;
        }

        return new("typeid") { Prefix = prefix };
    }

    static bool ValidPrefix(string prefix) => prefix.Length == 0 || IsTypeIdPrefix(prefix);

    static bool IsTypeIdPrefix(string prefix) => prefix.Length <= 63 && HasLetterEndpoints(prefix) && prefix.All(IsPrefixCharacter);

    static bool HasLetterEndpoints(string prefix) => IsLetter(prefix[0]) && IsLetter(prefix[^1]);

    static bool IsPrefixCharacter(char character) => IsLetter(character) || character == '_';

    static bool IsLetter(char character) => character is >= 'a' and <= 'z';

    static GenerationModel? NanoId(IInvocationOperation call)
    {
        if (Value(call, "size") is not int size || size is < 1 or > 256)
        {
            return null;
        }
        var alphabet = Value(call, "alphabet") as string;
        if (alphabet is not null && !ValidAlphabet(alphabet))
        {
            return null;
        }

        return new("nanoid") { Size = size == 21 ? null : size, Alphabet = alphabet };
    }

    static bool ValidAlphabet(string alphabet) => alphabet.Length is >= 2 and <= 256 && alphabet.All(character => character is >= ' ' and <= '~') && alphabet.Distinct().Count() == alphabet.Length;

    static GenerationModel? Bytes(IInvocationOperation call)
    {
        if (Value(call, "count") is not int count || count is < 1 or > 512)
        {
            return null;
        }
        var encoding = Value(call, "encoding") switch
        {
            0 => "base64url",
            1 => "hex",
            2 => "base32",
            _ => null
        };

        return encoding is null ? null : new("bytes") { Count = count, Encoding = encoding };
    }

    static bool ValidHexLength(int length) => length is >= 2 and <= 1024 && length % 2 == 0;

    static GenerationModel? Hex(IInvocationOperation call)
    {
        if (!HasOnly(call, "stringLength", "lowercase") || Value(call, "stringLength") is not int length || !ValidHexLength(length))
        {
            return null;
        }

        return new("bytes") { Count = length / 2, Encoding = "hex", Case = Value(call, "lowercase") is true ? null : "upper" };
    }
}
