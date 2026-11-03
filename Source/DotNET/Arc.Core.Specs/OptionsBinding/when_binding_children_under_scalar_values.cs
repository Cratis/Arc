// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when child keys are placed under scalar and abstract members.
/// </summary>
public class when_binding_children_under_scalar_values : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Tenancy:HttpHeader:Child"] = "x",
        ["Cratis:Arc:Query:KeepAliveInterval:Ticks"] = "5",
        ["Cratis:Arc:Query:KeepAliveInterval:Days"] = "x",
        ["Cratis:Arc:IdentityDetailsProvider:Name"] = "x",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Endpoint:Port"] = "x",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Endpoint:Host"] = "x"
    };
}
