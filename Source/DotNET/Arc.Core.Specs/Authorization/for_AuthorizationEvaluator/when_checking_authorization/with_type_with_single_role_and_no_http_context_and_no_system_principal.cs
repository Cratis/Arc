// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_authorization;

public class with_type_with_single_role_and_no_http_context_and_no_system_principal : given.an_authorization_helper
{
    bool _result;

    void Establish() => SetupNoHttpRequestContext();

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithSingleRole));

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
