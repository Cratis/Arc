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

describe('when rendering in strict mode', () => {
    afterEach(() => cleanup());

    it('should replay registration with the same command and execute it only once', async () => {
        const scope = new CommandScopeImplementation(() => {});
        const add = sinon.spy(scope, 'addCommand');
        const remove = sinon.spy(scope, 'removeCommand');
        let command!: FakeCommand;
        const Editor = () => {
            [command] = useCommand(FakeCommand);
            return null;
        };
        const view = render(React.createElement(React.StrictMode, null,
            React.createElement(ArcContext.Provider, { value: { microservice: 'test' } },
                React.createElement(CommandScopeContext.Provider, { value: scope }, React.createElement(Editor)))));
        const execute = sinon.stub(command, 'execute').resolves(CommandResult.empty);

        add.callCount.should.equal(remove.callCount + 1);
        add.callCount.should.equal(2);
        add.getCalls().every(call => call.args[0] === command).should.be.true;
        remove.calledOnceWithExactly(command).should.be.true;
        act(() => {
            command.someProperty = 'draft';
            command.propertyChanged('someProperty');
        });
        await act(async () => { await scope.execute(); });

        execute.calledOnce.should.be.true;
        view.unmount();
        remove.callCount.should.equal(add.callCount);
        scope.hasChanges.should.be.false;
    });
});
