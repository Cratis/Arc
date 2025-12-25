// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Internal command class for testing internal command support.
/// </summary>
[Command]
internal class InternalCommand
{
    public string Value { get; set; } = string.Empty;

    public void Handle()
    {
    }
}

/// <summary>
/// Public command with internal handle method for testing.
/// </summary>
[Command]
public class PublicCommandWithInternalHandle
{
    public string Value { get; set; } = string.Empty;

    internal void Handle()
    {
    }
}
