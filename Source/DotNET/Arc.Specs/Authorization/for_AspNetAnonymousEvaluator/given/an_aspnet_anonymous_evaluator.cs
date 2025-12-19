// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AspNetAnonymousEvaluator.given;

public class an_aspnet_anonymous_evaluator : Specification
{
    protected AspNetAnonymousEvaluator _evaluator;

    void Establish() => _evaluator = new AspNetAnonymousEvaluator();
}
