// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandScopeImplementation } from '../CommandScopeImplementation.js';
import { FakeCommand } from './FakeCommand.js';

describe('when removing a command with changes', () => {
    it('should execute only the remaining command', async () => {
        const scope = new CommandScopeImplementation(() => {});
        const removed = new FakeCommand(true);
        const remaining = new FakeCommand(true);
        scope.addCommand(removed);
        scope.addCommand(remaining);

        scope.removeCommand(removed);
        await scope.execute();

        removed.execute.called.should.be.false;
        remaining.execute.calledOnce.should.be.true;
    });

    it('should clear has changes after removing the only changed command', () => {
        const setHasChanges = sinon.spy();
        const scope = new CommandScopeImplementation(setHasChanges);
        const command = new FakeCommand(true);
        scope.addCommand(command);
        scope.addCommand(new FakeCommand(false));

        scope.removeCommand(command);

        scope.hasChanges.should.be.false;
        setHasChanges.lastCall.calledWith(false).should.be.true;
    });
});
