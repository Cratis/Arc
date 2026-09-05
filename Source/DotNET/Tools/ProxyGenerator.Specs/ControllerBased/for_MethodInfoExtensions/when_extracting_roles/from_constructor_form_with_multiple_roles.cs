// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_MethodInfoExtensions.when_extracting_roles;

public class from_constructor_form_with_multiple_roles : Specification
{
    MethodInfo _method;
    IEnumerable<string> _result;

    void Establish() => _method = typeof(RoleSecuredTypes).GetMethod(nameof(RoleSecuredTypes.MultipleRolesFromConstructor));

    void Because() => _result = _method.GetRoles();

    [Fact] void should_yield_all_roles() => _result.ShouldContainOnly(["Librarian", "Admin"]);
}
