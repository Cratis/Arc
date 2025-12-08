// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_method_authorization;

public class with_method_without_authorization_and_type_with_authorization_and_authenticated_user : given.an_authorization_helper
{
    bool _result;

    void Establish() => SetupAuthenticatedUser();

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithAuthorizationAndMethodWithoutAuthorization).GetMethod(nameof(given.TypeWithAuthorizationAndMethodWithoutAuthorization.Method))!);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
