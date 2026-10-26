// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { FakeObservableQuery } from '../../queries/for_useObservableQuery/FakeObservableQuery.js';

export class ObserveInvoices extends FakeObservableQuery {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Observing');
}
