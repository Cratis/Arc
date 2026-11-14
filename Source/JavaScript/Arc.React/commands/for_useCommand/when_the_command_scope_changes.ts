// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, cleanup, render } from '@testing-library/react';
import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { ArcContext } from '../../ArcContext.js';
import { CommandScopeContext } from '../CommandScopeContext.js';
import { CommandScopeImplementation } from '../CommandScopeImplementation.js';
import { useCommand } from '../useCommand.js';
import { FakeCommand } from './FakeCommand.js';

describe('when the command scope changes', () => {
    afterEach(() => cleanup());

    it('should move the same command from the old scope to the new scope', async () => {
        const oldScope = new CommandScopeImplementation(() => {});
        const newScope = new CommandScopeImplementation(() => {});
        const remove = sinon.spy(oldScope, 'removeCommand');
        const add = sinon.spy(newScope, 'addCommand');
        let command!: FakeCommand;
        const Editor = () => {
            [command] = useCommand(FakeCommand);
            return null;
        };
        const tree = (scope: CommandScopeImplementation) => React.createElement(
            ArcContext.Provider, { value: { microservice: 'test' } },
            React.createElement(CommandScopeContext.Provider, { value: scope }, React.createElement(Editor)));
        const view = render(tree(oldScope));
        const originalCommand = command;
        const execute = sinon.stub(command, 'execute').resolves(CommandResult.empty);
        act(() => {
            command.someProperty = 'draft';
            command.propertyChanged('someProperty');
        });

        view.rerender(tree(newScope));

        command.should.equal(originalCommand);
        remove.calledOnceWithExactly(command).should.be.true;
        add.calledOnceWithExactly(command).should.be.true;
        oldScope.hasChanges.should.be.false;
        newScope.hasChanges.should.be.true;
        await act(async () => { await oldScope.execute(); });
        execute.called.should.be.false;
        await act(async () => { await newScope.execute(); });
        execute.calledOnce.should.be.true;
    });
});
