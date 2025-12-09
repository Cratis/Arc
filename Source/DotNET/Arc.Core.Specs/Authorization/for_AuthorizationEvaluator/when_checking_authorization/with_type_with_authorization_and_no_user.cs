// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_authorization;

public class with_type_with_authorization_and_no_user : given.an_authorization_helper
{
    bool _result;

    void Establish() => SetupNoUser();

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithAuthorization));

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}