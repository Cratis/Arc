// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

internal static class DecisionPolicyForSpecs
{
    internal static IDisposable Begin(Type commandType) => (IDisposable)typeof(CommandDecisionPolicy)
        .GetMethod("Begin", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, [commandType])!;
}
