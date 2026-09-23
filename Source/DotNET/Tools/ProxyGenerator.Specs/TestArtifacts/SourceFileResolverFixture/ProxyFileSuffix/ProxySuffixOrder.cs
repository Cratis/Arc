// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.ProxyFileSuffixFixture;

/// <summary>
/// A type that imports another generated type, and one mapped to a hand-written module.
/// </summary>
public class ProxySuffixOrder
{
    /// <summary>
    /// Gets or sets the line, a generated type the proxy imports.
    /// </summary>
    public ProxySuffixLine Line { get; set; } = new();

    /// <summary>
    /// Gets or sets the total, mapped to a hand-written TypeScript module rather than generated.
    /// </summary>
    public decimal Total { get; set; }
}
