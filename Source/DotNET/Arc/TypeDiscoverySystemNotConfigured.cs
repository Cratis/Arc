// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Exception that gets thrown when the service provider has not been configured.
/// </summary>
public class TypeDiscoverySystemNotConfigured() :
    Exception("Type discovery system has not been configured, have you forgotten to call 'AddCratisArc()' on your host builder during setup?");
