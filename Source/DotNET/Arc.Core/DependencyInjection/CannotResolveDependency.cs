// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// The exception that is thrown when a dependency required by a command, provide method, validator, or query
/// cannot be resolved from the service provider.
/// </summary>
/// <remarks>
/// This differs from a dependency that is simply unregistered and declared as nullable (which resolves to null).
/// Here resolution actually fails — most commonly because the requested service exists but activating it fails
/// deeper in the graph. The classic cause is calling AddCratisArc() without WithChronicle() while a command,
/// provide method, validator, or query depends on a Chronicle service such as IEventLog, whose own dependencies
/// only WithChronicle() registers.
/// </remarks>
public class CannotResolveDependency : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CannotResolveDependency"/> class for a reflected parameter.
    /// </summary>
    /// <param name="parameter">The parameter whose dependency could not be constructed.</param>
    /// <param name="failure">The underlying failure captured while resolving the dependency.</param>
    public CannotResolveDependency(ParameterInfo parameter, Exception failure)
        : base(
            BuildMessage(
                $"'{parameter.ParameterType.FullName}' for parameter '{parameter.Name}' of '{parameter.Member.DeclaringType?.FullName}.{parameter.Member.Name}'",
                "The service is registered, but constructing it failed because one of its own dependencies could not be resolved.",
                parameter.ParameterType,
                failure),
            failure)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CannotResolveDependency"/> class for a service type.
    /// </summary>
    /// <param name="serviceType">The service type that could not be resolved.</param>
    /// <param name="failure">The underlying failure captured while resolving the dependency.</param>
    public CannotResolveDependency(Type serviceType, Exception failure)
        : base(
            BuildMessage(
                $"'{serviceType.FullName}'",
                "The service could not be constructed — it may be unregistered, or one of its own dependencies is missing.",
                serviceType,
                failure),
            failure)
    {
    }

    static string BuildMessage(string subject, string explanation, Type serviceType, Exception failure)
    {
        var message = $"Failed to resolve dependency {subject}. {explanation} See the inner exception for details.";

        if (ChronicleConfigurationHint.AppliesTo(serviceType, failure))
        {
            message = $"{message} {ChronicleConfigurationHint.Text}";
        }

        return message;
    }
}
