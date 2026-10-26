// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand } from '../ICommand.js';
import { PayInvoice } from './PayInvoice.js';
import { SomeCommand } from './SomeCommand.js';
import { SliceLocation } from '../../reflection/SliceLocation.js';

describe('when getting the slice location', () => {
    let declared: SliceLocation | undefined;
    let undeclared: SliceLocation | undefined;

    beforeEach(() => {
        const command: ICommand = new PayInvoice();
        declared = SliceLocation.of(command);
        undeclared = SliceLocation.of(new SomeCommand());
    });

    it('should return the static slice location of the proxy class', () => declared!.should.equal(PayInvoice.sliceLocation));
    it('should return the empty slice location when the class declares none', () => undeclared!.should.equal(SliceLocation.empty));
});
