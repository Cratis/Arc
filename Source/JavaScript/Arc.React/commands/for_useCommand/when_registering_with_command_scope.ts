// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { useCommand } from '../useCommand';
import { FakeCommand } from './FakeCommand';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { CommandScope } from '../CommandScope';
import { CommandScopeImplementation } from '../CommandScopeImplementation';

describe('when registering with command scope', () => {
    let addCommandSpy: sinon.SinonSpy;
    let consoleErrorStub: sinon.SinonStub;

    class GeneratedCommand extends FakeCommand {
        static use() {
            return useCommand(GeneratedCommand);
        }
    }

    const TestComponent = () => {
        GeneratedCommand.use();
        return React.createElement('div', null, 'Test');
    };

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    beforeEach(() => {
        addCommandSpy = sinon.spy(CommandScopeImplementation.prototype, 'addCommand');
        consoleErrorStub = sinon.stub(console, 'error');
    });

    afterEach(() => {
        addCommandSpy.restore();
        consoleErrorStub.restore();
    });

    it('should register the command with the scope', async () => {
        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    CommandScope,
                    null,
                    React.createElement(TestComponent)
                )
            )
        );

        await waitFor(() => addCommandSpy.calledOnce.should.be.true);
    });

    it('should not warn about updating the scope while rendering', async () => {
        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    CommandScope,
                    null,
                    React.createElement(TestComponent)
                )
            )
        );

        await waitFor(() => addCommandSpy.calledOnce.should.be.true);

        const renderWarnings = consoleErrorStub.getCalls().filter(call =>
            call.args.some(argument => typeof argument === 'string' && argument.includes('Cannot update a component'))
        );

        renderWarnings.should.be.empty;
    });
});
