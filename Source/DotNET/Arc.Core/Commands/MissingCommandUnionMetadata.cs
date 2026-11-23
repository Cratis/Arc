// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Exception thrown before command execution when a native union has no generated declaration metadata
/// and the untrimmed JIT reflection compatibility path is disabled.
/// </summary>
/// <param name="unionType">The union requiring generated metadata.</param>
public class MissingCommandUnionMetadata(Type unionType) : Exception(
    $"Native union '{unionType}' has no generated command union metadata. Reference the Arc source generator and expose a nameable closed union in the command's declared return graph. Union reflection is disabled for trimmed and NativeAOT applications.");
