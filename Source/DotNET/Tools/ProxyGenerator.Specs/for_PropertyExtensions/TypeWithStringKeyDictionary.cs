// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions;

public class TypeWithStringKeyDictionary
{
    public IDictionary<string, DictionaryValueType> Items { get; set; } = new Dictionary<string, DictionaryValueType>();
}
