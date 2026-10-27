// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { FakeQuery } from '../../queries/for_useQuery/FakeQuery.js';

export class AllInvoices extends FakeQuery {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Listing');
}
