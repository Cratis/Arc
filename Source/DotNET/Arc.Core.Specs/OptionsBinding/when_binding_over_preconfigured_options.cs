// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when options were configured in code before the configuration is bound.
/// </summary>
public class when_binding_over_preconfigured_options : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Tenancy:HttpHeader"] = "from-config",
        ["Cratis:Arc:EndpointGroups:Ports:admin:0"] = "9"
    };

    protected override Action<ArcOptions>? Prepare => options =>
    {
        options.Tenancy.BaseDomain = "kept";
        options.EndpointGroups.Ports["admin"] = [1, 2];
        options.Introspection.RequireAuthentication = true;
    };
}
