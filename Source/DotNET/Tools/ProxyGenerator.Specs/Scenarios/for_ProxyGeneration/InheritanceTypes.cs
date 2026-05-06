// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class ScenarioBaseType
{
    public string SharedProperty { get; set; } = string.Empty;
}

public class ScenarioDerivedType : ScenarioBaseType
{
    public int DerivedProperty { get; set; }
}

public class ScenarioSystemBaseType : System.Collections.Generic.List<string>
{
    public string OwnProperty { get; set; } = string.Empty;
}
