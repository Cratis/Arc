// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Subtype setters run in runtime member order, before inherited setters.
/// </summary>
public class when_binding_a_subtype_with_order_dependent_members : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:OnBehalfOf:Extra"] = "configured",
        ["Cratis:Arc:OnBehalfOf:Issuer"] = "issuer"
    };

    protected override Action<ArcOptions> Prepare => options => options.OnBehalfOf = new OrderedOptions();

    class OrderedOptions : OnBehalfOfOptions
    {
        public string Extra
        {
            get => Service;
            set => Service = $"{value}:{Issuer}";
        }
    }
}
