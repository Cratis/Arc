// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionScopeSnapshot.given;

/// <summary>
/// A scope whose value is serialized but cannot be restored, because the property has no setter.
/// </summary>
public class ScopeLosingDataOnRoundTrip
{
    public ScopeLosingDataOnRoundTrip()
    {
    }

    public ScopeLosingDataOnRoundTrip(int value) => Value = value;

    public int Value { get; }
}
