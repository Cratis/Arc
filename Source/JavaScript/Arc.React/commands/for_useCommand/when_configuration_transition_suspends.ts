// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { startTransition, useContext, useState } from 'react';
import { act, fireEvent, render, RenderResult } from '@testing-library/react';
import sinon from 'sinon';
import { Globals } from '@cratis/arc';
import { CommandResult } from '@cratis/arc/commands';
import { ArcConfiguration, ArcContext } from '../../ArcContext.js';
import { useCommand } from '../useCommand.js';
import { ConfiguredCommand } from './ConfiguredCommand.js';

describe('when a command configuration transition suspends', () => {
    let fetchStub: sinon.SinonStub;
    let view: RenderResult;
    let suspended: boolean;
    let initialCommand: ConfiguredCommand;
    let currentCommand: ConfiguredCommand;
    let pending: Promise<CommandResult>;

    beforeEach(async () => {
        suspended = false;
        fetchStub = sinon.stub(globalThis, 'fetch').resolves({
            status: 200,
            json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString() })
        } as Response);
        const committed: ArcConfiguration = {
            microservice: 'committed-service', apiBasePath: '/committed', origin: 'https://committed.example',
            httpHeadersCallback: () => ({ 'X-Custom': 'committed' })
        };
        const next: ArcConfiguration = {
            microservice: 'next-service', apiBasePath: '/next', origin: 'https://next.example',
            httpHeadersCallback: () => ({ 'X-Custom': 'next' })
        };
        const waiting = new Promise<void>(() => { /* Keep the transition uncommitted. */ });
        let updateConfiguration!: React.Dispatch<React.SetStateAction<ArcConfiguration>>;

        function Consumer() {
            const config = useContext(ArcContext);
            const [command] = useCommand(ConfiguredCommand);
            currentCommand = command;
            if (config === next) {
                suspended = true;
                throw waiting;
            }
            return React.createElement('button', { onClick: () => { pending = command.execute(); } }, config.origin);
        }

        function App() {
            const [config, setConfig] = useState(committed);
            updateConfiguration = setConfig;
            return React.createElement(ArcContext.Provider, { value: config },
                React.createElement(React.Suspense, { fallback: 'loading' }, React.createElement(Consumer)));
        }

        view = render(React.createElement(App));
        initialCommand = currentCommand;
        await act(async () => {
            startTransition(() => updateConfiguration(next));
        });
        await act(async () => {
            fireEvent.click(view.getByRole('button', { name: committed.origin }));
            await pending;
        });
    });

    afterEach(() => {
        view.unmount();
        fetchStub.restore();
    });

    it('should attempt the suspended render', () => suspended.should.be.true);
    it('should retain the visible committed UI', () => view.getByRole('button').textContent!.should.equal('https://committed.example'));
    it('should retain the command identity', () => currentCommand.should.equal(initialCommand));
    it('should execute against the committed origin and base path', () => String(fetchStub.firstCall.args[0]).should.equal('https://committed.example/committed/save'));
    it('should execute with the committed microservice', () => fetchStub.firstCall.args[1].headers[Globals.microserviceHttpHeader].should.equal('committed-service'));
    it('should execute with the committed headers callback', () => fetchStub.firstCall.args[1].headers['X-Custom'].should.equal('committed'));
});
