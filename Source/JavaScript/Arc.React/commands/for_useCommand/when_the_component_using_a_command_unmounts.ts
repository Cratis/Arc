// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, cleanup, render } from '@testing-library/react';
import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { ArcContext } from '../../ArcContext.js';
import { CommandScope } from '../CommandScope.js';
import { ICommandScope } from '../ICommandScope.js';
import { useCommandScope } from '../useCommandScope.js';
import { useCommand } from '../useCommand.js';
import { FakeCommand } from './FakeCommand.js';

describe('when the component using a command unmounts', () => {
    afterEach(() => cleanup());

    it('should discard the abandoned draft and execute only a remounted editor command', async () => {
        let command!: FakeCommand;
        let scope!: ICommandScope;
        const Editor = () => {
            [command] = useCommand(FakeCommand);
            return null;
        };
        const ScopeObserver = () => {
            scope = useCommandScope();
            return null;
        };
        const tree = (showEditor: boolean) => React.createElement(
            ArcContext.Provider,
            { value: { microservice: 'test' } },
            React.createElement(CommandScope, null,
                React.createElement(ScopeObserver),
                showEditor ? React.createElement(Editor) : React.createElement(React.Fragment))
        );
        const view = render(tree(true));
        const survivingScope = scope;
        const remove = sinon.spy(scope, 'removeCommand');
        const abandoned = command;
        const abandonedExecute = sinon.stub(abandoned, 'execute').resolves(CommandResult.empty);
        const request = sinon.stub(globalThis, 'fetch').rejects(new Error('No request should be sent'));
        try {
            act(() => {
                abandoned.someProperty = 'abandoned draft';
                abandoned.propertyChanged('someProperty');
            });
            scope.hasChanges.should.be.true;

            view.rerender(tree(false));
            scope.should.equal(survivingScope);
            remove.calledOnceWithExactly(abandoned).should.be.true;
            await act(async () => { await scope.execute(); });

            abandonedExecute.called.should.be.false;
            request.called.should.be.false;
            scope.hasChanges.should.be.false;

            view.rerender(tree(true));
            command.should.not.equal(abandoned);
            const remountedExecute = sinon.stub(command, 'execute').resolves(CommandResult.empty);
            act(() => {
                command.someProperty = 'new draft';
                command.propertyChanged('someProperty');
            });
            await act(async () => { await scope.execute(); });

            remountedExecute.calledOnce.should.be.true;
            abandonedExecute.called.should.be.false;
            request.called.should.be.false;
        } finally {
            request.restore();
        }
    });
});
