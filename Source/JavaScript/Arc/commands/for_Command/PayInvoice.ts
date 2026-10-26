// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command.js';
import { PropertyDescriptor } from '../../reflection/PropertyDescriptor.js';
import { SliceLocation } from '../../reflection/SliceLocation.js';

export class PayInvoice extends Command {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Pay');
    readonly route: string = '/api/billing/invoices/pay';
    readonly propertyDescriptors: PropertyDescriptor[] = [];

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }
}
