// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when nothing is configured.
/// </summary>
public class when_binding_no_configuration : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>();
}
