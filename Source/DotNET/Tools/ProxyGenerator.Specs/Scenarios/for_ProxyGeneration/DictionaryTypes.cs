// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class ScenarioDictionaryKeyType
{
    public string Id { get; set; } = string.Empty;
}

public class ScenarioDictionaryValueType
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TypeWithStringKeyDictionaryProperty
{
    public IDictionary<string, ScenarioDictionaryValueType> Items { get; set; } = new Dictionary<string, ScenarioDictionaryValueType>();
}

public class TypeWithComplexKeyDictionaryProperty
{
    public IDictionary<ScenarioDictionaryKeyType, ScenarioDictionaryValueType> Items { get; set; } = new Dictionary<ScenarioDictionaryKeyType, ScenarioDictionaryValueType>();
}
