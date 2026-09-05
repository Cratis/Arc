// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Establishes the process-wide MongoDB defaults used by the specification assembly.
/// </summary>
static class MongoDBSpecificationAssembly
{
    /// <summary>
    /// Initializes Arc's application defaults before any specification can resolve a MongoDB serializer.
    /// </summary>
    /// <remarks>
    /// MongoDB's serializer registry is process-wide and freezes a default serializer the first time a type is
    /// resolved. Initializing at module load makes every scenario observe production initialization independently of
    /// test discovery order.
    /// </remarks>
    [ModuleInitializer]
    internal static void InitializeMongoDBDefaults() => new ServiceCollection().AddCratisMongoDB();
}
