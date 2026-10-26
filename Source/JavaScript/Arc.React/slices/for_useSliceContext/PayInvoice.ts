// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { FakeCommand } from '../../commands/for_useCommand/FakeCommand.js';

export class PayInvoice extends FakeCommand {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Pay');
}
