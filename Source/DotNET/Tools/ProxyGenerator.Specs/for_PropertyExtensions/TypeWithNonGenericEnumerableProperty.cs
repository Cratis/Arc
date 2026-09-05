// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions;

// This type intentionally implements only the non-generic IEnumerable to reproduce the bug
// where GetEnumerableElementType() returns null for types without IEnumerable<T>.
#pragma warning disable CA1010
public class NonGenericEnumerable : IEnumerable
#pragma warning restore CA1010
{
    public IEnumerator GetEnumerator() => throw new NotSupportedException();
}

public class TypeWithNonGenericEnumerableProperty
{
    public NonGenericEnumerable Items { get; set; } = new();
}
