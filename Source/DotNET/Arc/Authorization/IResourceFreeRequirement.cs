// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Marks an authorization requirement whose handlers only read the user and never read the resource.
/// </summary>
/// <remarks>
/// Arc may evaluate policies consisting entirely of resource-free requirements with a null resource when command body binding fails.
/// Do not implement this interface for requirements that inspect command properties or use the resource indirectly.
/// </remarks>
public interface IResourceFreeRequirement : IAuthorizationRequirement;
