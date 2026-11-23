// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using OneOf;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Inspects the bindings that precede parameter resolvers and DI for Handle only.</summary>
/// <param name="command">The owning command.</param>
sealed class CommandWiringHandleBindings(Type command)
{
    /// <summary>Checks runtime-supplied arguments and possible Provide candidates without creating values.</summary>
    /// <param name="parameter">The Handle parameter.</param>
    /// <returns>A binding verdict, or not applicable for resolver/DI dependencies.</returns>
    public CommandWiringVerdict Inspect(ParameterInfo parameter)
    {
        if (parameter.IsDefined(typeof(GeneratedValueAttribute), inherit: false))
        {
            return InspectGenerated(parameter.ParameterType);
        }
        if (parameter.ParameterType == typeof(CancellationToken))
        {
            return CommandWiringVerdict.Supported;
        }
        if (!CouldProvide(parameter.ParameterType))
        {
            return CommandWiringVerdict.NotApplicable;
        }
        return LookUpByKeyProviderDeclarations.For(parameter).Any(LookUpByKeyProviderDeclarations.IsProvider)
            ? new(true, "lookup", "A Provide result cannot replace a caller-keyed read model; verify the binding in a command scenario.")
            : new(true, "context-required", "Provide may supply this argument before resolvers and DI; verify its result in a command scenario without guessing a value here.");
    }

    static bool CouldBind(Type candidate, Type parameter) => parameter.IsAssignableFrom(candidate) || candidate.IsAssignableFrom(parameter);

    static IEnumerable<Type> Candidates(Type type)
    {
        if (HasNoCandidate(type))
        {
            return [];
        }
        if (IsAwaited(type))
        {
            return type.GenericTypeArguments.SelectMany(Candidates);
        }
        if (IsComposite(type))
        {
            return type.GenericTypeArguments.SelectMany(Candidates).Append(type);
        }
        return IsOpaqueComposite(type) ? [typeof(object)] : [type];
    }

    static bool HasNoCandidate(Type type) => type == typeof(void) || type == typeof(Task) || type == typeof(ValueTask) || IsControl(type);

    static bool IsControl(Type type) => typeof(CommandResult).IsAssignableFrom(type) ||
        typeof(ValidationResult).IsAssignableFrom(type) || typeof(IEnumerable<ValidationResult>).IsAssignableFrom(type) ||
        typeof(AuthorizationResult).IsAssignableFrom(type);

    static bool IsAwaited(Type type) => type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>));

    static bool IsComposite(Type type) => type.IsGenericType && (IsFrameworkTuple(type) || IsFrameworkUnion(type));

    static bool IsFrameworkTuple(Type type) => type.Assembly == typeof(ITuple).Assembly && typeof(ITuple).IsAssignableFrom(type);

    static bool IsFrameworkUnion(Type type) => type.Assembly == typeof(IOneOf).Assembly && typeof(IOneOf).IsAssignableFrom(type);

    static bool IsOpaqueComposite(Type type) => typeof(ITuple).IsAssignableFrom(type) || typeof(IOneOf).IsAssignableFrom(type);

    static CommandWiringVerdict InspectGenerated(Type dependency) =>
        GeneratedValueParameters.IsGeneratableInContext(dependency)
            ? new(true, "context-required", "Context-dependent generation requires the command's receipt time and tenant; check it in a command scenario.")
            : CommandWiringVerdict.Supported;

    bool CouldProvide(Type dependency)
    {
        var provide = command.GetProvideMethod();
        return provide is not null && Candidates(provide.ReturnType).Any(type => CouldBind(type, dependency));
    }
}
