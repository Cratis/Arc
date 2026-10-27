// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { FakeSuspenseQuery } from '../../queries/for_useSuspenseQuery/FakeSuspenseQuery.js';

export class SuspenseInvoices extends FakeSuspenseQuery {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Suspending');
}
