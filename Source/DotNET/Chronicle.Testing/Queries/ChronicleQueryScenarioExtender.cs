// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Queries;

/// <summary>
/// Adds in-memory Chronicle read model seeding to query scenarios.
/// </summary>
public class ChronicleQueryScenarioExtender : IQueryScenarioExtender
{
    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context) =>
        new ChronicleCommandScenarioExtender().Extend(services, context);
}
