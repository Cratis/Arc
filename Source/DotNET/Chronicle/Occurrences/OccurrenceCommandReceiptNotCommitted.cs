// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when a command executed for an occurrence committed events but no effect receipt was
/// reported with the commit. The executor fails closed instead of reporting success.
/// </summary>
/// <param name="effectKey">The key of the effect.</param>
public class OccurrenceCommandReceiptNotCommitted(string effectKey)
    : Exception($"The command for effect '{effectKey}' committed events but the effect receipt was not reported with the commit.");
