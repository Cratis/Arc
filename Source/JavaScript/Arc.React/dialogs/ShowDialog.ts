// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResponse } from './DialogResponse.js';

/**
 * Represents a function that shows a dialog and returns a promise that resolves with the dialog response.
 */
export type ShowDialog<TProps, TResponse = object> = (props?: TProps) => Promise<DialogResponse<TResponse>>;