// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor } from './IObservableQueryFor';

/**
 * Defines a change-stream-capable observable query. Any {@link IObservableQueryFor} returning
 * an enumerable ({@code TDataType[]}) satisfies this interface automatically, enabling it to
 * be used with the {@code useChangeStream} React hook which delivers per-update deltas
 * (added / replaced / removed) instead of the full collection.
 * @template TDataType The element type of the observable collection (not the array itself).
 */
// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface IChangeStreamFor<TDataType> extends IObservableQueryFor<TDataType[]> {}
