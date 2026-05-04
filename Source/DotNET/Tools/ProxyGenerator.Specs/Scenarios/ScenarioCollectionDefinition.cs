// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios;

[CollectionDefinition(Name)]
public class ScenarioCollectionDefinition : ICollectionFixture<SharedScenarioWebHostFixture>
{
    public const string Name = "Scenario WebApplication Tests";
}
