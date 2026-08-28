// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// These integration specs share one disposable package graph and local NuGet feed. Run the assembly sequentially
// so fixture construction, consumer builds, code-fix hosts, and temporary cleanup cannot race one another.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
