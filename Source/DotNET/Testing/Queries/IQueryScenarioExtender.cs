// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Extends query scenarios with services and disposable context values before initialization.
/// </summary>
public interface IQueryScenarioExtender
{
    /// <summary>
    /// Configures a new query scenario.
    /// </summary>
    /// <param name="services">The scenario's service registrations.</param>
    /// <param name="context">The scenario's context values.</param>
    void Extend(IServiceCollection services, IDictionary<string, object> context);
}
