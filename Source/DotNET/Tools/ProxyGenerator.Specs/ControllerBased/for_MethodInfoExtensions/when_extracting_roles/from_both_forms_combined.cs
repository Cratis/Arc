// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_MethodInfoExtensions.when_extracting_roles;

public class from_both_forms_combined : Specification
{
    MethodInfo _method;
    IEnumerable<string> _result;

    void Establish() => _method = typeof(RoleSecuredTypes).GetMethod(nameof(RoleSecuredTypes.RoleFromBothForms));

    void Because() => _result = _method.GetRoles();

    [Fact] void should_deduplicate_the_roles() => _result.ShouldContainOnly(["Librarian"]);
}
