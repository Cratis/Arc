// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public sealed class EventTypeAttribute : Attribute;
