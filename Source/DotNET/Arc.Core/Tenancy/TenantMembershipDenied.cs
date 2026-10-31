// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Exception representing a tenant membership denial.
/// </summary>
public sealed class TenantMembershipDenied() : Exception("The caller is not a member of the selected tenant.")
{
    /// <summary>
    /// Checks whether an exception or one of its nested causes is a tenant membership denial.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>True if a tenant membership denial is found within sixteen nesting levels.</returns>
    public static bool IsCauseOf(Exception? exception) => IsCauseOf(exception, 16);

    static bool IsCauseOf(Exception? exception, int remaining) => remaining > 0 && exception switch
    {
        TenantMembershipDenied => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(inner => IsCauseOf(inner, remaining - 1)),
        { InnerException: { } inner } => IsCauseOf(inner, remaining - 1),
        _ => false
    };
}
