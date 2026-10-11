// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

public class a_generated_value_scenario : Specification
{
    protected CommandScenario<AllocateCartId> _scenario;

    void Establish() => _scenario = new();
    void Destroy() => _scenario.Dispose();
}
