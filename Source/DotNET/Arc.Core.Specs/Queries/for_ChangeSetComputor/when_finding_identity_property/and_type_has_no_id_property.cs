// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_finding_identity_property;

public class and_type_has_no_id_property : Specification
{
    PropertyInfo? _result;

    void Because() => _result = ChangeSetComputor.FindIdentityProperty(typeof(given.ItemWithoutId));

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
