// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when unknown keys are present.
/// </summary>
public class when_binding_unknown_keys : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Nothing:Here"] = "x",
        ["Cratis:Arc:Tenancy:Unknown"] = "x"
    };
}
