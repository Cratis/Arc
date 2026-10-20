// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when command key lookup requires generated metadata for an unregistered type.
/// </summary>
/// <param name="type">The command or read model type without metadata.</param>
public sealed class CommandReadModelKeysMissing(Type type)
    : Exception($"Type '{type}' has no generated command read-model key metadata. Enable the Arc source generator. ReadProtection:RequireGeneratedMetadata requires registration; runtimes without dynamic code support always require it.");
