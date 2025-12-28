// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AnonymousEvaluator.given;

public class an_anonymous_evaluator : Specification
{
    protected AnonymousEvaluator _evaluator;

    void Establish() => _evaluator = new AnonymousEvaluator();
}
