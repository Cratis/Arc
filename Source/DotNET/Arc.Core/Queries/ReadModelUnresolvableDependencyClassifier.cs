// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an <see cref="IUnresolvableDependencyClassifier"/> that recognizes a non-nullable read model dependency
/// which does not exist for the command's resolved key as invalid client input rather than a server fault.
/// </summary>
/// <remarks>
/// The classifier only runs when a non-nullable dependency resolved to null. For a read model resolved through an
/// <see cref="ICanResolveReadModelForCommand"/>, a null result can only mean the entity does not exist for a usable key:
/// a command carrying no usable key throws <see cref="UnableToResolveReadModelFromCommandContext"/> inside the resolver
/// before this point. It therefore classifies a registered read model dependency with a usable resolved key as a
/// <see cref="ReadModelDoesNotExistForCommand"/> (HTTP 400). Any dependency that is not a registered read model, or where
/// no usable key is available, is left for the default server-error behavior so misconfigurations are not masked.
/// </remarks>
[Singleton]
public class ReadModelUnresolvableDependencyClassifier : IUnresolvableDependencyClassifier
{
    /// <inheritdoc/>
    public bool TryClassifyAsClientInput(ParameterInfo parameter, IServiceProvider serviceProvider, [NotNullWhen(true)] out Exception? failure)
    {
        failure = null;

        var registeredReadModelTypes = serviceProvider.GetService<RegisteredReadModelTypes>();
        if (registeredReadModelTypes?.Contains(parameter.ParameterType) != true)
        {
            return false;
        }

        // The command context registered in DI carries no service provider of its own, so the key is read through the
        // one classifying — the same scope the read model failed to resolve in.
        var commandContext = serviceProvider.GetService<CommandContext>();
        if (commandContext is null || string.IsNullOrEmpty(commandContext.GetResolvedKey(serviceProvider)))
        {
            return false;
        }

        failure = new ReadModelDoesNotExistForCommand(parameter.ParameterType);
        return true;
    }
}
