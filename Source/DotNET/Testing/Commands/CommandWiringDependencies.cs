// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Preflights parameter declarations and ordinary dependency activation.</summary>
/// <param name="command">The owning command.</param>
/// <param name="services">The scoped provider.</param>
/// <param name="options">The extension rules.</param>
/// <param name="problems">The collected failures.</param>
sealed class CommandWiringDependencies(Type command, IServiceProvider services, CommandWiringOptions options, List<CommandWiringProblem> problems)
{
    /// <summary>Checks one dependency without executing parameter resolvers.</summary>
    /// <param name="parameter">The dependency declaration.</param>
    /// <param name="isValidator">Whether the dependency belongs to a validator.</param>
    /// <returns>Whether its binding can be verified.</returns>
    public bool Inspect(ParameterInfo parameter, bool isValidator)
    {
        try
        {
            var context = new CommandWiringParameter(command, parameter, services, isValidator, CommandDecisionPolicy.IsProtected, CommandDecisionPolicy.ReadProtection);
            var verdict = options.Rules.Select(rule => rule.Inspect(context)).FirstOrDefault(_ => _.IsApplicable);
            if (verdict?.Category is { } category)
            {
                Add(parameter, category, verdict.Guidance!, verdict.Cause);
                return false;
            }
            if (verdict is not null && !isValidator)
            {
                if (CommandDecisionPolicy.IsProtected)
                {
                    Add(parameter, "unsupported", "Protected contextual bindings require invocation state; check them in a command scenario.");
                    return false;
                }
                return true;
            }
            return InspectService(parameter, isValidator && verdict is not null);
        }
        catch (Exception failure)
        {
            Add(parameter, "activation", "Dependency activation or protected dependency validation failed; inspect Cause for the exception type.", failure);
            return false;
        }
    }

    static bool IsAvailable(ParameterInfo parameter, object? value, bool contextual) =>
        value is not null || (!contextual && new NullabilityInfoContext().Create(parameter).WriteState == NullabilityState.Nullable);

    bool InspectService(ParameterInfo parameter, bool contextual)
    {
        if (LookUpByKeyProviderDeclarations.For(parameter).Any(LookUpByKeyProviderDeclarations.IsProvider) && !contextual)
        {
            Add(parameter, "lookup", "Caller-derived key providers require a Chronicle read-model binding.");
            return false;
        }
        var value = services.GetService(parameter.ParameterType);
        if (CommandDecisionPolicy.IsProtected)
        {
            services.GetRequiredService<ICommandProtectedDecisionSupport>().ValidateCommandDependency(parameter.ParameterType, value);
        }
        if (IsAvailable(parameter, value, contextual))
        {
            return true;
        }
        var guidance = contextual
            ? $"Register an explicit {parameter.ParameterType.FullName} fixture for the validator constructor; no model will be read."
            : $"Register {parameter.ParameterType.FullName} in the supplied scoped provider.";
        Add(parameter, contextual ? "context-required" : "missing-service", guidance);
        return false;
    }

    void Add(ParameterInfo parameter, string category, string guidance, Exception? cause = null) =>
        problems.Add(new(
            command,
            parameter.Member is ConstructorInfo ? $"{parameter.Member.DeclaringType!.FullName}.ctor" : parameter.Member.Name,
            $"({parameter.Name}: {parameter.ParameterType.FullName})",
            category,
            guidance,
            cause));
}
