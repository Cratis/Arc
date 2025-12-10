// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Cratis.Arc.Execution;

/// <summary>
/// Represents an implementation of <see cref="ILogEnricher"/> that enriches logs with the current correlation ID.
/// </summary>
/// <param name="correlationIdAccessor">The accessor for the current correlation ID.</param>
public class CorrelationIdLogEnricher(ICorrelationIdAccessor correlationIdAccessor) : ILogEnricher
{
    /// <inheritdoc/>
    public void Enrich(IEnrichmentTagCollector collector)
    {
        collector.Add("CorrelationId", correlationIdAccessor.Current.ToString());
    }
}
