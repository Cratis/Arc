// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents one generated document and what the catalog says about it.
/// </summary>
/// <param name="Document">The catalog entry describing the document.</param>
/// <param name="Source">The printed <c>.play</c> text.</param>
public record GeneratedDocument(EmbeddedDocument Document, string Source);
