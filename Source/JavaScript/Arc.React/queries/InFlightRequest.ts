// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * A request in flight on a shared query instance, with the serialized arguments it was performed with.
 */
export type InFlightRequest = { readonly request: Promise<unknown>; readonly argumentsKey: string };
