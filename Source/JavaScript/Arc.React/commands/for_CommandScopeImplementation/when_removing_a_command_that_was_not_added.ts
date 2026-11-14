// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandScopeImplementation } from '../CommandScopeImplementation.js';
import { FakeCommand } from './FakeCommand.js';

describe('when removing a command that was not added', () => {
    it('should leave the scope unchanged', async () => {
        const setHasChanges = sinon.spy();
        const scope = new CommandScopeImplementation(setHasChanges);
        const command = new FakeCommand(true);
        scope.addCommand(command);
        setHasChanges.resetHistory();

        scope.removeCommand(new FakeCommand(true));

        setHasChanges.called.should.be.false;
        scope.hasChanges.should.be.true;
        scope.hasValidationFailures.should.be.false;
        scope.hasExceptions.should.be.false;
        await scope.execute();
        command.execute.calledOnce.should.be.true;
    });
});
