// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { PropertyChanged } from '@cratis/arc/commands';
import { FakeCommand } from './FakeCommand';

describe('when property changes on added command', () => {
    const setHasChanges = sinon.stub();
    const scope = new CommandScopeImplementation(setHasChanges);
    let callbackToCall: PropertyChanged;
    let thisArgForCallback: object = {};

    const command = new FakeCommand(true);
    command.onPropertyChanged.callsFake((callback: PropertyChanged, thisArg: object): void => {
        callbackToCall = callback;
        thisArgForCallback = thisArg;
    });

    scope.addCommand(command);

    callbackToCall!.call(thisArgForCallback, '');

    it('should call set has changes', () => setHasChanges.called.should.be.true);
});
