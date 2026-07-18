// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.Events;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Represents an <see cref="IUnresolvableDependencyClassifier"/> that recognizes a non-nullable read model dependency
/// which does not exist for the command's event source id as invalid client input rather than a server fault.
/// </summary>
/// <remarks>
/// The classifier only runs when a non-nullable dependency resolved to null. For a read model that is invoked through
/// <see cref="ReadModelServiceCollectionExtensions.ResolveReadModel"/>, a null result can only mean the entity does not
/// exist for a valid event source id: an unspecified id throws <see cref="UnableToResolveReadModelFromCommandContext"/>
/// before this point. It therefore classifies a registered read model dependency with a valid event source id as a
/// <see cref="ReadModelDoesNotExistForCommand"/> (HTTP 400). Any dependency that is not a registered read model, or where
/// no event source id is available, is left for the default server-error behavior so misconfigurations are not masked.
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

        var commandContext = serviceProvider.GetService<CommandContext>();
        if (commandContext is null || commandContext.GetEventSourceId() == EventSourceId.Unspecified)
        {
            return false;
        }

        failure = new ReadModelDoesNotExistForCommand(parameter.ParameterType);
        return true;
    }
}
