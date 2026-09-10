// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Marks business data declaring command work. Implement one public instance Execute method and optionally
/// Compensate, returning void, Task, or ValueTask. Method parameters receive scoped services and cancellation.
/// </summary>
public interface ICommandOperation;
