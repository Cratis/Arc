// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// The exception that is thrown when MongoDB asks for an access token and the connection resolution no longer gives one.
/// </summary>
/// <param name="request">The <see cref="ConnectionRequest"/> that was resolved.</param>
public class MongoDBAccessTokenNotResolved(ConnectionRequest request)
    : Exception($"MongoDB asked for an access token, but resolving the connection for '{request.ToKey()}' gave none");
