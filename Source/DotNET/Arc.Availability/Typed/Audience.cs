// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Base type of an audience: a named group a part of the application is available to.
/// </summary>
/// <remarks>
/// Declare an audience as a type, <c>public class Plus : Audience;</c>, and refer to it with a type argument:
/// <c>[AvailableFor&lt;Plus&gt;]</c>. The name of the type is the name of the audience, so a typo is a compile error.
/// The type says who the audience is for and nothing else; membership is granted at run time.
/// </remarks>
public abstract class Audience;
