// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandKeys;

/// <summary>
/// A rule an application adds of its own, resolving a fixed key for every command.
/// </summary>
/// <param name="key">The key to resolve.</param>
public class an_application_rule(string? key) : ICanResolveKeyForCommand
{
    public string? Resolve(object command) => key;
}
