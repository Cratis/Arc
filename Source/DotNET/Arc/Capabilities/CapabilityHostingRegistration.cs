// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Capabilities;

/// <summary>Records explicit capability hosting, independently of convention-created service bindings.</summary>
[IgnoreConvention]
internal sealed class CapabilityHostingRegistration;
