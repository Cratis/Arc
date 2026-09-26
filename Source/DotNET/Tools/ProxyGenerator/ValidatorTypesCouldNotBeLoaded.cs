// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// The exception that is thrown when an assembly's validator types cannot all be loaded.
/// </summary>
/// <param name="assembly">The assembly whose types could not be loaded.</param>
/// <param name="exception">The reflection failure including its loader exceptions.</param>
internal class ValidatorTypesCouldNotBeLoaded(Assembly assembly, ReflectionTypeLoadException exception)
    : Exception($"Could not load all validator types from assembly '{assembly.GetName().Name}': {string.Join("; ", exception.LoaderExceptions.Where(_ => _ is not null).Select(_ => _.Message))}", exception);
