// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions;

public class TypeWithComplexKeyDictionary
{
    public IDictionary<DictionaryKeyType, DictionaryValueType> Items { get; set; } = new Dictionary<DictionaryKeyType, DictionaryValueType>();
}
