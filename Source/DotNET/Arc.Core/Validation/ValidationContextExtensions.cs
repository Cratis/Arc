// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Extensions for <see cref="IValidationContext"/>.
/// </summary>
public static class ValidationContextExtensions
{
    const string TypeKey = "Type";
    const string Command = "Command";
    const string Query = "Query";

    /// <summary>
    /// Set the context as a command.
    /// </summary>
    /// <param name="context"><see cref="IValidationContext"/> to set for.</param>
    public static void SetCommand(this IValidationContext context)
    {
        context.RootContextData[TypeKey] = Command;
    }

    /// <summary>
    /// Set the context as a query.
    /// </summary>
    /// <param name="context"><see cref="IValidationContext"/> to set for.</param>
    public static void SetQuery(this IValidationContext context)
    {
        context.RootContextData[TypeKey] = Query;
    }

    /// <summary>
    /// Check if the context is for a command.
    /// </summary>
    /// <param name="context"><see cref="IValidationContext"/> to check.</param>
    /// <returns>True if it is a command, false if not.</returns>
    public static bool IsCommand(this IValidationContext context)
    {
        return (context.RootContextData[TypeKey] as string ?? string.Empty) == Command;
    }

    /// <summary>
    /// Check if the context is for a query.
    /// </summary>
    /// <param name="context"><see cref="IValidationContext"/> to check.</param>
    /// <returns>True if it is a query, false if not.</returns>
    public static bool IsQuery(this IValidationContext context)
    {
        return (context.RootContextData[TypeKey] as string ?? string.Empty) == Query;
    }
}