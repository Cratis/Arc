// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Exception that gets thrown when the endpoint mapper has not been configured.
/// </summary>
public class EndpointMapperNotConfigured : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="EndpointMapperNotConfigured"/>.
    /// </summary>
    public EndpointMapperNotConfigured() : base("Endpoint mapper has not been configured, have you forgotten to call 'UseCratisArc()' on your application during setup?")
    {
    }
}
