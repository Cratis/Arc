// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionIdentity;

public class when_converting_to_string : Specification
{
    string _result;

    void Because() => _result = new ConnectionIdentity("user", "secret-password", "secret-token").ToString();

    [Fact] void should_include_the_user_name() => _result.ShouldContain("user");
    [Fact] void should_not_include_the_password() => _result.ShouldNotContain("secret-password");
    [Fact] void should_not_include_the_access_token() => _result.ShouldNotContain("secret-token");
}
