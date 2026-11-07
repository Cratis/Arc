// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.Features;

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Keeps outside admission valid across endpoint reassignment and pipeline re-execution.
/// </summary>
/// <param name="selection">The frozen startup selection.</param>
/// <param name="current">The positively outside endpoint admitted by the gate.</param>
/// <param name="refusal">The endpoint that responds with an admission refusal.</param>
internal sealed class CapabilityOutsideEndpointFeature(CapabilityEndpointSelection selection, Endpoint current, Endpoint refusal) : IEndpointFeature
{
    Endpoint? _endpoint = current;

    /// <inheritdoc/>
    public Endpoint? Endpoint
    {
        get => _endpoint;
        set
        {
            if (ReferenceEquals(_endpoint, refusal))
            {
                return;
            }

            // Clearing permits routing to run again, but only another known outside endpoint may execute.
            _endpoint = value is null || selection.IsOutside(value) ? value : refusal;
        }
    }
}
