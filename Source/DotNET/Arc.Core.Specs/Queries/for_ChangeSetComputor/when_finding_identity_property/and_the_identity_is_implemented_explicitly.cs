// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_finding_identity_property;

public class and_the_identity_is_implemented_explicitly : Specification
{
    PropertyInfo? _result;

    void Because() => _result = ChangeSetComputor.FindIdentityProperty(typeof(given.ItemWithExplicitId));

    [Fact] void should_find_the_interface_property() => _result.DeclaringType.ShouldEqual(typeof(given.IHaveAnIdentity));
    [Fact] void should_find_property_named_id() => _result.Name.ShouldEqual("Id");
}
