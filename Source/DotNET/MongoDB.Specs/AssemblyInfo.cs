// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Several specs configure MongoDB through process-wide static state — most notably the naming policy on
// DatabaseExtensions and the registered convention packs. Running test classes in parallel races on that
// shared state and produces intermittent failures, so parallelization is disabled for this assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
