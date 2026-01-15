// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions;

public class TypeWithNullableReferenceProperties
{
    public string? NullableString { get; set; }
    public string NonNullableString { get; set; } = string.Empty;
    public object? NullableObject { get; set; }
}
