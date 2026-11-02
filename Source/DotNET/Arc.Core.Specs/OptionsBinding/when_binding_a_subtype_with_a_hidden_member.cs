// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// A subtype's hidden property must bind instead of the declared base property, without duplicating inherited collections.
/// </summary>
public class when_binding_a_subtype_with_a_hidden_member : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:OnBehalfOf:Issuer"] = "configured",
        ["Cratis:Arc:OnBehalfOf:SigningKeys:0:KeyId"] = "key"
    };

    protected override Action<ArcOptions> Prepare => options => options.OnBehalfOf = new HiddenIssuerOptions();

    class HiddenIssuerOptions : OnBehalfOfOptions
    {
        public new string Issuer { get; set; } = "initial";
    }
}
