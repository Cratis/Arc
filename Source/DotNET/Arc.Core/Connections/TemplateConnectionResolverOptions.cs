// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the options for <see cref="TemplateConnectionResolver"/>.
/// </summary>
/// <remarks>
/// Each template can use the scope values of the request as placeholders in braces, for instance <c>{tenant}</c>, as
/// well as <c>{purpose}</c> and <c>{storage}</c>. A template that is not set leaves that part as configured.
/// </remarks>
public class TemplateConnectionResolverOptions
{
    /// <summary>
    /// Gets or sets the connection string template.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the database name template.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the schema name template.
    /// </summary>
    public string? Schema { get; set; }
}
