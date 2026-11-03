// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when a setter throws.
/// </summary>
public class when_binding_a_setter_that_rejects_the_value : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:JsonSerializerOptions:MaxDepth"] = "-1"
    };
}
