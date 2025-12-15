// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AnonymousEvaluator;

public class when_checking_type_with_authorize : given.an_anonymous_evaluator
{
    bool? _result;

    void Because() => _result = _evaluator.IsAnonymousAllowed(typeof(given.TypeWithAuthorize));

    [Fact] void should_return_false() => _result.Value.ShouldBeFalse();
}
