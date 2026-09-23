// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // The file is deliberately named differently from its type, so source-file output renames the module.
namespace Cratis.Arc.ProxyGenerator.Specs.ProxyFileSuffixFixture;

/// <summary>
/// A type declared in a file named differently from itself, so source-file output renames its module.
/// </summary>
public class ProxySuffixLine
{
    /// <summary>
    /// Gets or sets the stock keeping unit.
    /// </summary>
    public string Sku { get; set; } = string.Empty;
}
#pragma warning restore SA1649 // The file is deliberately named differently from its type, so source-file output renames the module.
