// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides extension methods for working with subjects on commands and tuples.
/// </summary>
public static class SubjectExtensions
{
    /// <summary>
    /// Determines whether the command has an explicit subject.
    /// </summary>
    /// <param name="command">The command to inspect.</param>
    /// <returns>True if the command has a subject; otherwise false.</returns>
    public static bool HasSubject(this object command) =>
        ResolveSubject(command) is not null;

    /// <summary>
    /// Determines whether the tuple has a subject.
    /// </summary>
    /// <param name="tuple">The tuple to inspect.</param>
    /// <returns>True if the tuple has a subject; otherwise false.</returns>
    public static bool HasSubject(this ITuple tuple) =>
        ResolveSubject(tuple) is not null;

    /// <summary>
    /// Gets the subject from the command, if present.
    /// </summary>
    /// <param name="command">The command to inspect.</param>
    /// <returns>The subject if found; otherwise null.</returns>
    public static Subject? GetSubject(this object command) =>
        ResolveSubject(command);

    /// <summary>
    /// Gets the subject from the tuple, if present.
    /// </summary>
    /// <param name="tuple">The tuple to inspect.</param>
    /// <returns>The subject if found; otherwise null.</returns>
    public static Subject? GetSubject(this ITuple tuple) =>
        ResolveSubject(tuple);

    static Subject? ResolveSubject(object command)
    {
        if (command is ITuple tuple)
        {
            return ResolveSubject(tuple);
        }

        var property =
            command.GetType().GetProperties().FirstOrDefault(IsSubjectProperty) ??
            ResolvePropertyFromConstructorParameter(command.GetType());

        return property is not null
            ? ToSubject(property.GetValue(command))
            : null;
    }

    static Subject? ResolveSubject(ITuple tuple)
    {
        for (var i = 0; i < tuple.Length; i++)
        {
            var subject = ToSubject(tuple[i]);
            if (subject is not null)
            {
                return subject;
            }
        }

        return null;
    }

    static bool IsSubjectProperty(PropertyInfo property) =>
        property.PropertyType == typeof(Subject) ||
        Attribute.IsDefined(property, typeof(SubjectAttribute));

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "GetConstructors/GetProperty on user command types; their public members are preserved. Source-generated dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    static PropertyInfo? ResolvePropertyFromConstructorParameter(Type type)
    {
        var constructor = type.GetConstructors().FirstOrDefault();
        if (constructor is null)
        {
            return null;
        }

        var parameter = constructor.GetParameters().FirstOrDefault(p =>
            p.ParameterType == typeof(Subject) ||
            Attribute.IsDefined(p, typeof(SubjectAttribute)));

        return parameter is not null
            ? type.GetProperty(parameter.Name!, BindingFlags.Public | BindingFlags.Instance)
            : null;
    }

    static Subject? ToSubject(object? value) =>
        value switch
        {
            Subject subject => subject,
            null => null,
            _ => new Subject(value.ToString()!)
        };
}
