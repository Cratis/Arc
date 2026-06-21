// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// These integration specs shell out to `dotnet pack` and `dotnet build` against the same in-repo projects and
// their shared build outputs. Running them concurrently races on those outputs and the NuGet restore, so the
// whole assembly must run sequentially.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
