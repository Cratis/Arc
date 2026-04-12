// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Defines an interface for extending a <see cref="CommandScenario{TCommand}"/> with additional services and context values.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are discovered automatically by <see cref="CommandScenario{TCommand}"/> at construction time
/// using the <see cref="Types.ITypes"/> type discovery system. Any class in any loaded assembly
/// that implements this interface will be instantiated (via a parameterless constructor) and its
/// <see cref="Extend"/> method called before the first command is executed.
/// </para>
/// <para>
/// Use this interface to register additional services into the test service collection and store
/// objects in the string-keyed context dictionary. Extension packages can then expose those objects through
/// C# extension properties on <see cref="CommandScenario{TCommand}"/>.
/// </para>
/// </remarks>
public interface ICommandScenarioExtender
{
    /// <summary>
    /// Extends the scenario by registering services and populating the context.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the scenario.</param>
    /// <param name="context">The scenario context dictionary, keyed by <see cref="string"/>.</param>
    void Extend(IServiceCollection services, IDictionary<string, object> context);
}
