// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents an event reaching a slice from outside the application.
/// </summary>
/// <param name="Id">The identity of the external event.</param>
/// <param name="Name">The name of the external event.</param>
/// <param name="SourceEventId">The identity of the event it stands for.</param>
/// <remarks>
/// A generated document never holds one: a Screenplay document declares every event it uses, so an event a
/// slice consumes is carried as an event referencing the slice that produces it.
/// </remarks>
public record ExternalEventItem(Guid Id, string Name, string SourceEventId);
