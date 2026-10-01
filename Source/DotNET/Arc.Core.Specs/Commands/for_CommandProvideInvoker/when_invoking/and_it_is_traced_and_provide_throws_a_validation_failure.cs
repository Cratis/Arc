// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

/// <summary>
/// A validation failure thrown from Provide() becomes a validation outcome for the command, not an error.
/// </summary>
public class and_it_is_traced_and_provide_throws_a_validation_failure : given.a_traced_command_provide_invoker
{
    public class RegisterAuthor
    {
        public string Provide() => throw new UnableToResolveReadModelFromCommandContext(typeof(object));
    }

    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () => await Invoke(new RegisterAuthor()));

    [Fact] void should_let_the_failure_through() => _error.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
    [Fact] void should_leave_the_status_unset() => ProvideSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_exception_type() => ProvideSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).Tags.Single().Value.ShouldEqual(typeof(UnableToResolveReadModelFromCommandContext).FullName);
}
