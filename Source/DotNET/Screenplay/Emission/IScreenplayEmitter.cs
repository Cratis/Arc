// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Defines a system that turns an application model into a Screenplay document.
/// </summary>
/// <remarks>
/// This is the half of the generator that knows the Screenplay language. It sees only the model, never a
/// compilation, which is what makes it exercisable by handing it records and reading back the document.
/// </remarks>
public interface IScreenplayEmitter
{
    /// <summary>
    /// Emits the Screenplay document describing a model.
    /// </summary>
    /// <param name="model">The model to emit.</param>
    /// <param name="options">The options to emit with.</param>
    /// <returns>The <see cref="ScreenplayEmission"/>.</returns>
    ScreenplayEmission Emit(ApplicationModel model, ScreenplayOptions options);
}
