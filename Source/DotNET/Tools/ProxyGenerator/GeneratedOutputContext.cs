// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Combines artifact output and progress reporting.</summary>
/// <param name="Message">The progress callback.</param>
/// <param name="Output">The artifact output boundary.</param>
internal sealed record GeneratedOutputContext(Action<string> Message, IGeneratedOutput Output);
