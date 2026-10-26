// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { addSliceLocation } from '../addSliceLocation.js';

export const location = { module: 'Billing', feature: 'Invoices', slice: 'Pay' };

export const transform = (code: string, file: string = '/app/Features/Billing/Invoices/Pay/PayDialog.tsx') => addSliceLocation(code, file, location)?.code;

export const wrapped = (expression: string) => `__cratisWithSliceLocation(__cratisSliceLocation, (${expression}))`;
