// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Artifacts stays in the whole-graph parity comparison: the stock binder cannot populate its get-only read-only collection,
/// and Arc deliberately has no binding table for this code-only host opt-in.
/// </summary>
public class when_binding_code_only_artifacts : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:Artifacts:IncludedAssemblies:0"] = "Cratis.Arc.Core.Specs"
    };
}
