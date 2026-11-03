// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when a key is explicitly null and also has child keys.
/// </summary>
public class when_binding_an_explicit_null_with_children : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Tenancy:HttpHeader"] = null,
        ["Cratis:Arc:Tenancy:HttpHeader:Length"] = "invalid"
    };
}
