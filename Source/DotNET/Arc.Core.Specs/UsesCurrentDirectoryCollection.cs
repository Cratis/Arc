// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Collection definition for specs that mutate <see cref="Environment.CurrentDirectory"/>.
/// </summary>
[CollectionDefinition("UsesCurrentDirectory", DisableParallelization = true)]
public class UsesCurrentDirectoryCollection;