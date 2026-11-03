// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// The members MongoDB observation reads by reflection on a document type.
/// </summary>
internal static class DocumentTypeMembers
{
    /// <summary>
    /// The member types a generic document parameter must preserve: the public instance properties used to read the id and sort properties.
    /// </summary>
    internal const DynamicallyAccessedMemberTypes Required = DynamicallyAccessedMemberTypes.PublicProperties;
}
