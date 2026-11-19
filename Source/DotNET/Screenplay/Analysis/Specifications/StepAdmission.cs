// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// What reading one call of a specification's steps decided.
/// </summary>
internal enum StepAdmission
{
    /// <summary>The call is not one the read is about.</summary>
    Skip = 0,

    /// <summary>The call is read.</summary>
    Read = 1,

    /// <summary>The scenario cannot be read, so reading stops.</summary>
    Stop = 2
}
