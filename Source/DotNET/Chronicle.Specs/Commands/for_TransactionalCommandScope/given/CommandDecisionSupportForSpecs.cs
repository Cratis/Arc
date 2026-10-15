// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;

public abstract class CommandDecisionSupportForSpecs : ICommandProtectedDecisionSupport
{
    public abstract void EnsureSupported(IServiceProvider services);
    public abstract void ValidateCommandDependency(Type dependencyType, object? dependency);
    public abstract IDisposable? BeginMethodBody(MethodInfo method);
}
