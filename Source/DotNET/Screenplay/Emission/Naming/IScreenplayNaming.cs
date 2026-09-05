// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Defines a system that converts model names and free text into forms the Screenplay language accepts.
/// </summary>
/// <remarks>
/// The Screenplay printer performs no escaping and no case conversion. Every name and every string that ends up in
/// the output must therefore be normalized before a syntax node is constructed.
/// </remarks>
public interface IScreenplayNaming
{
    /// <summary>
    /// Converts a member name to the lower camel case form required for property, parameter and mapping names.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The camel cased name.</returns>
    string ToPropertyName(string name);

    /// <summary>
    /// Converts a dotted member path to the lower camel case form, one segment at a time.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The camel cased path.</returns>
    string ToPropertyPath(string path);

    /// <summary>
    /// Converts a type name to the identifier form required for declaration names.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The sanitized identifier.</returns>
    string ToDeclarationName(string name);

    /// <summary>
    /// Removes every character that would make a Screenplay string literal fail to parse.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The sanitized value, or <see langword="null"/> when nothing is left.</returns>
    string? ToStringLiteral(string? value);

    /// <summary>
    /// Converts a source file path into the form a <c>file</c> reference accepts.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The sanitized path, or <see langword="null"/> when nothing is left.</returns>
    string? ToFilePath(string? path);
}
