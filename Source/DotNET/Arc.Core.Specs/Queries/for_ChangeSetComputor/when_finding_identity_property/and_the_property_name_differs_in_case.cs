// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_finding_identity_property;

public class and_the_property_name_differs_in_case : Specification
{
    PropertyInfo? _result;

    void Because() => _result = ChangeSetComputor.FindIdentityProperty(typeof(ItemWithUpperCaseId));

    [Fact] void should_find_the_property() => _result.Name.ShouldEqual("ID");

    public record ItemWithUpperCaseId(int ID);
}
