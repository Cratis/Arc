// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Arc's own option types are fully described by the binding tables, so the runtime type fallback never runs for them.
/// </summary>
public class when_binding_arc_own_option_types : Specification
{
    int _bindCount;

    void Because()
    {
        RuntimeTypeFallback.ResetBindCount();
        _ = new ParityRun(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Tenancy:HttpHeader"] = "X-Tenant",
            ["Cratis:Arc:JsonSerializerOptions:WriteIndented"] = "true",
            ["Cratis:Arc:OnBehalfOf:SigningKeys:0:KeyId"] = "k0",
            ["Cratis:Arc:EndpointGroups:RoutePrefixes:admin"] = "/admin",
            ["Cratis:Arc:Introspection:RequireAuthentication"] = "true"
        });
        _bindCount = RuntimeTypeFallback.BindCount;
    }

    [Fact] void should_not_run_the_fallback() => _bindCount.ShouldEqual(0);
}
