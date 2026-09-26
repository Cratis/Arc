// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SomeCommand } from './SomeCommand.js';

describe('when clearing the command', () => {
    const command = new SomeCommand();
    command.setInitialValues({
        someProperty: ''
    });
    command.someProperty = '42';
    command.propertyChanged('someProperty');
    command.clear();

    it('should not have any changes', () => command.hasChanges.should.be.false);
    it('should clear the property', () => (command.someProperty === undefined).should.be.true);
});
