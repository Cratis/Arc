// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidationContextOperationExtensions;

public class when_reading_a_retained_context_after_completion : for_OperationSnapshot.given.an_operation_snapshot_environment
{
    Exception _tenantError;
    Exception _causedByError;
    Exception _systemError;

    void Because()
    {
        var snapshot = OperationSnapshot.Begin(_services, () => null);
        var retained = new ValidationContext<object>(new object());
        retained.RootContextData[OperationSnapshot.ContextKey] = snapshot;
        snapshot.Dispose();

        _tenantError = Catch.Exception(() => retained.GetTenant());
        _causedByError = Catch.Exception(() => retained.GetCausedBy());
        _systemError = Catch.Exception(() => retained.IsSystem());
    }

    [Fact] void should_throw_for_the_tenant() => _tenantError.ShouldBeOfExactType<OperationSnapshotUnavailable>();
    [Fact] void should_throw_for_the_caller() => _causedByError.ShouldBeOfExactType<OperationSnapshotUnavailable>();
    [Fact] void should_throw_for_system() => _systemError.ShouldBeOfExactType<OperationSnapshotUnavailable>();
}
