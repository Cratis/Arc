// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a nested command enabling read protection cannot keep its caller's read regions
/// out, because the Chronicle client in use does not support suspending them.
/// </summary>
/// <remarks>
/// Arc no longer throws this exception: it suspends inherited read regions through
/// <see cref="Cratis.Chronicle.Transactions.IUnitOfWork.SuspendReadRegions"/>, which every supported Chronicle client provides.
/// </remarks>
[Obsolete("Arc no longer throws this exception; nested commands suspend read regions through IUnitOfWork.SuspendReadRegions(). It will be removed in the next major version.")]
public class ReadRegionsCannotBeSuspended()
    : Exception("A nested command with read protection On cannot run: the Chronicle client in use cannot keep its caller's read allowances out of it. Use the Chronicle version Arc was built against.");
