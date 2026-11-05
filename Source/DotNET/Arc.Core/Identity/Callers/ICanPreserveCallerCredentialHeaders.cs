// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A host's explicit guarantee that separate credential header lines remain separate values.
/// Absence or false means refusal; a flattened dictionary or last-value reader cannot implement this guarantee.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal interface ICanPreserveCallerCredentialHeaders
{
    /// <summary>Gets a value indicating whether separate credential lines are preserved without joining or replacement.</summary>
    bool PreservesDuplicateCredentialHeaders { get; }
}
