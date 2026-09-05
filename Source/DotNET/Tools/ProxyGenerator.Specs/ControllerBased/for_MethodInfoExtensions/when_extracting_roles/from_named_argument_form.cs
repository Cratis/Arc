// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_MethodInfoExtensions.when_extracting_roles;

public class from_named_argument_form : Specification
{
    MethodInfo _method;
    IEnumerable<string> _result;

    void Establish() => _method = typeof(RoleSecuredTypes).GetMethod(nameof(RoleSecuredTypes.RoleFromNamedArgument));

    void Because() => _result = _method.GetRoles();

    [Fact] void should_yield_the_role() => _result.ShouldContainOnly(["Librarian"]);
}
