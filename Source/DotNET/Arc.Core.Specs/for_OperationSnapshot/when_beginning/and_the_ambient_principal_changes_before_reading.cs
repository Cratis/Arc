// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Identity;

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class and_the_ambient_principal_changes_before_reading : given.an_operation_snapshot_environment
{
    CausingIdentity _causedBy;
    bool _isSystem;

    void Because()
    {
        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        _principals.Current.Returns(SystemPrincipal.WithRoles());
        _causedBy = snapshot.CausedBy;
        _isSystem = snapshot.IsSystem;
    }

    [Fact] void should_still_be_the_user() => _causedBy.Subject.Value.ShouldEqual("user-1");
    [Fact] void should_not_be_system() => _isSystem.ShouldBeFalse();
}
