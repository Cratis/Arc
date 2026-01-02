// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_generating_proxy_for_command_with_complex_response : given.a_scenario_web_application
{
    void Establish() => LoadCommandProxy<CommandWithComplexResponse>("/tmp/command-with-complex-response-generated.ts");

    [Fact] void should_generate_without_errors() => true.ShouldBeTrue();
}
