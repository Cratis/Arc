// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_finding_identity_property;

public class after_hot_reload_clears_the_caches : Specification
{
    record ItemWithIdentity(Guid Id, string Name);

    PropertyInfo? _beforeClearing;
    PropertyInfo? _afterClearing;

    void Establish() => _beforeClearing = ChangeSetComputor.FindIdentityProperty(typeof(ItemWithIdentity));

    void Because()
    {
        ChangeSetComputorMetadataUpdateHandler.ClearCache([typeof(ItemWithIdentity)]);
        _afterClearing = ChangeSetComputor.FindIdentityProperty(typeof(ItemWithIdentity));
    }

    [Fact] void should_find_the_identity_again() => _afterClearing!.Name.ShouldEqual(nameof(ItemWithIdentity.Id));
    [Fact] void should_resolve_the_same_property() => _afterClearing.ShouldEqual(_beforeClearing);
}
