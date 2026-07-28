// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a screen of a slice, realized by a file rather than described declaratively.
/// </summary>
/// <param name="Name">The name of the screen, as the file realizing it is named.</param>
/// <param name="FilePath">The path of the file realizing the screen, relative to the root of the source.</param>
/// <remarks>
/// A screen is the one part of a slice whose realization is not C#, so the only thing recovered about it is which
/// file realizes it. Saying that much is worth doing - it is what lets a reader open the screen a slice ends in -
/// and saying more would mean inventing structure that nothing in the source states.
/// </remarks>
public record ScreenModel(string Name, string FilePath);
