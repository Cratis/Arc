// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Describes a query argument for template purposes.
/// </summary>
/// <param name="OriginalType">The original type of the argument.</param>
/// <param name="Name">Name of argument.</param>
/// <param name="Type">Type of argument.</param>
/// <param name="Constructor">The JavaScript constructor for the type.</param>
/// <param name="IsOptional">Whether or not the argument is nullable / optional.</param>
/// <param name="IsQueryStringParameter">Whether or not the argument is a query string parameter.</param>
/// <param name="Documentation">JSDoc documentation for the parameter.</param>
public record RequestParameterDescriptor(
    Type OriginalType,
    string Name,
    string Type,
    string Constructor,
    bool IsOptional,
    bool IsQueryStringParameter = false,
    string? Documentation = null);
