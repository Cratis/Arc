// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AspNetAuthorizationAttributeEvaluator.given;

public class an_aspnet_authorization_attribute_evaluator : Specification
{
    protected AspNetAuthorizationAttributeEvaluator _evaluator;

    void Establish() => _evaluator = new AspNetAuthorizationAttributeEvaluator();
}
