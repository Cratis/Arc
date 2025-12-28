// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationAttributeEvaluator.given;

public class an_authorization_attribute_evaluator : Specification
{
    protected AuthorizationAttributeEvaluator _evaluator;

    void Establish() => _evaluator = new AuthorizationAttributeEvaluator();
}
