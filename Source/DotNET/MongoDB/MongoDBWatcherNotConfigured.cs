// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB;

/// <summary>
/// The exception that is thrown when <see cref="MongoDBWatcher"/> is asked to watch the application's configured
/// database, but was registered without the MongoDB configuration that names it.
/// </summary>
/// <param name="missingService">Name of the service the watch needs.</param>
public class MongoDBWatcherNotConfigured(string missingService)
    : Exception($"The MongoDB watcher needs {missingService} to watch the application's configured database. Configure MongoDB with UseMongoDB, or observe a collection or database, which is watched on its own client.");
