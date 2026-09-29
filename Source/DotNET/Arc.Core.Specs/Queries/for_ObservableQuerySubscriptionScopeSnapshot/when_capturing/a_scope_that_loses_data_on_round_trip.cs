// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionScopeSnapshot.when_capturing;

public class a_scope_that_loses_data_on_round_trip : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        _ = new ObservableQuerySubscriptionScopeSnapshot(new given.ScopeLosingDataOnRoundTrip(42), new ArcOptions().JsonSerializerOptions));

    [Fact] void should_reject_the_scope() => _error.ShouldBeOfExactType<InvalidSubscriptionScope>();
    [Fact] void should_name_the_scope_type() => _error.Message.ShouldContain(nameof(given.ScopeLosingDataOnRoundTrip));
}
