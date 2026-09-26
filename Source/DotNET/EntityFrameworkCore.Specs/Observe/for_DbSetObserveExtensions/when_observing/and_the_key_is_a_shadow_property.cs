// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

public class and_the_key_is_a_shadow_property : a_db_set_observe_context
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _dbContext.ShadowKeyEntities.Observe());

    [Fact] void should_reject_the_key_before_observing() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_identify_the_shadow_property() => _error.Message.ShouldContain("ShadowId");
}
