// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// The exception that is thrown when synchronous authorization encounters a policy or an explicit authentication scheme.
/// </summary>
public class AsynchronousAuthorizationRequired() : Exception("Authorization policies and authentication schemes require the asynchronous command or query pipeline.");
