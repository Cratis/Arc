// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Represents the hosting options for Arc, only used by Arc.Core.
/// </summary>
public class HostingOptions
{
    /// <summary>
    /// Gets or sets the application URL.
    /// </summary>
    public string ApplicationUrl { get; set; } = "http://+:5001/";
}