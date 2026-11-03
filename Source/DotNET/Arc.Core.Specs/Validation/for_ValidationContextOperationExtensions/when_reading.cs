// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidationContextOperationExtensions;

public class when_reading : for_OperationSnapshot.given.an_operation_snapshot_environment
{
    Exception _tenantError;
    Exception _causedByError;
    Exception _systemError;
    TenantId _tenant;
    bool _isSystem;

    void Because()
    {
        var bare = new ValidationContext<object>(new object());
        _tenantError = Catch.Exception(() => bare.GetTenant());
        _causedByError = Catch.Exception(() => bare.GetCausedBy());
        _systemError = Catch.Exception(() => bare.IsSystem());

        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        var stamped = new ValidationContext<object>(new object());
        stamped.RootContextData[OperationSnapshot.ContextKey] = snapshot;
        _tenant = stamped.GetTenant();
        _isSystem = stamped.IsSystem();
    }

    [Fact] void should_throw_for_the_tenant_without_a_snapshot() => _tenantError.ShouldBeOfExactType<OperationSnapshotUnavailable>();
    [Fact] void should_throw_for_the_caller_without_a_snapshot() => _causedByError.ShouldBeOfExactType<OperationSnapshotUnavailable>();
    [Fact] void should_throw_for_system_without_a_snapshot() => _systemError.ShouldBeOfExactType<OperationSnapshotUnavailable>();
    [Fact] void should_read_the_tenant_from_the_snapshot() => _tenant.ShouldEqual(new TenantId("tenant-1"));
    [Fact] void should_read_system_from_the_snapshot() => _isSystem.ShouldBeFalse();
}
