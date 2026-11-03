// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when keys are explicitly null.
/// </summary>
public class when_binding_explicit_nulls : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Tenancy:HttpHeader"] = null,
        ["Cratis:Arc:ConnectionResolution:Connection:Template:Database"] = null,
        ["Cratis:Arc:Calendar"] = null,
        ["Cratis:Arc:EndpointGroups:Ports:admin"] = null,
        ["Cratis:Arc:ExposeExceptionDetails"] = null,
        ["Cratis:Arc:Query:AllowedWebSocketOrigins"] = null
    };
}
