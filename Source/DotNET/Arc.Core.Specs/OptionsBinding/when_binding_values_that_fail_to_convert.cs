// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when values can not be converted.
/// </summary>
public class when_binding_values_that_fail_to_convert : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Tenancy:ResolverType"] = "Nope",
        ["Cratis:Arc:Query:KeepAliveInterval"] = "soon",
        ["Cratis:Arc:ExposeExceptionDetails"] = "maybe"
    };
}
