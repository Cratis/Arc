// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// An absent intermediate scalar value must not become an explicit null, and conversion failures retain their full path.
/// </summary>
public class when_binding_a_subtype_with_nested_sections : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:OnBehalfOf:Extra:Length"] = "invalid",
        ["Cratis:Arc:OnBehalfOf:SigningKeys:0:KeyId"] = "key"
    };

    protected override Action<ArcOptions> Prepare => options => options.OnBehalfOf = new NestedOptions();

    class NestedOptions : OnBehalfOfOptions
    {
        public string Extra { get; set; } = "initial";
    }
}
