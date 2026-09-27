// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from './DialogResult.js';

/**
 * Represents the delegate when a dialog is closed with a response.
 */
export type CloseDialog<TResponse> = (result: DialogResult, response?: TResponse) => void;
