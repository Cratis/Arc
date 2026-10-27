// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor, SliceLocation } from '@cratis/arc/reflection';

export class PayInvoice extends Command {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Pay');
    readonly route = '/api/billing/invoices/pay';
    readonly propertyDescriptors: PropertyDescriptor[] = [];

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}
