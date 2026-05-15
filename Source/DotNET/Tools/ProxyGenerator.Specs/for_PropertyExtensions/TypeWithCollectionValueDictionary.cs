// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions;

public class TypeWithCollectionValueDictionary
{
    public IDictionary<string, IList<DictionaryValueType>> Slots { get; } = new Dictionary<string, IList<DictionaryValueType>>(StringComparer.Ordinal);
}
