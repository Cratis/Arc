// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators;

/// <summary>
/// Generates native union declaration metadata and typed active-value accessors for command returns.
/// </summary>
[Generator]
public class CommandUnionMetadataGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context) => CommandUnionDeclarations.Initialize(context);
}
