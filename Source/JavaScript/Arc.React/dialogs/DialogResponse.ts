// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from './DialogResult.js';

/**
 * Represents the response from a dialog, including the result and an optional response object.
 */
export type DialogResponse<TResponse = object> = [DialogResult, TResponse?];