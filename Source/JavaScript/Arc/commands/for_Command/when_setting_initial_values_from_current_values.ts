// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SomeCommand } from './SomeCommand.js';

for (const value of ['', 0, false, null, undefined, 'missing']) {
    describe(`when setting initial values from current value ${String(value)}`, () => {
        let command: SomeCommand;

        beforeEach(() => {
            command = new SomeCommand();
            command.setInitialValues({ someProperty: 'accepted' });
            if (value === 'missing') {
                Reflect.deleteProperty(command, 'someProperty');
            } else {
                command.someProperty = value as unknown as string;
            }
            command.setInitialValuesFromCurrentValues();
        });

        it('should mark the current values as clean', () => command.hasChanges.should.be.false);
        it('should revert to the captured value', () => {
            command.someProperty = 'later edit';
            command.revertChanges();
            (command.someProperty === (value === 'missing' ? undefined : value)).should.be.true;
        });
    });
}
