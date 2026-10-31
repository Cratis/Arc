// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult.when_creating_from_an_exception;

public class and_tenant_membership_is_denied : Specification
{
    CommandResult[] _results;
    CorrelationId _correlationId;
    void Because()
    {
        _correlationId = CorrelationId.New();
        Exception[] exceptions = [new TenantMembershipDenied(), new TargetInvocationException(new TenantMembershipDenied()), new AggregateException(new InvalidOperationException("ordinary"), new TenantMembershipDenied())];
        _results = exceptions.SelectMany(exception => new[] { CommandResult.FromException(_correlationId, exception), CommandResult.Error(_correlationId, exception) }).ToArray();
    }
    [Fact] void should_be_unauthorized() => _results.ShouldEachConformTo(result => !result.IsAuthorized);
    [Fact] void should_not_expose_exception_details() => _results.ShouldEachConformTo(result => !result.HasExceptions && result.ExceptionStackTrace.Length == 0);
    [Fact] void should_preserve_correlation() => _results.ShouldEachConformTo(result => result.CorrelationId == _correlationId);
}
