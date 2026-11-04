// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useContext, useEffect, useLayoutEffect } from 'react';
import { act, render, RenderResult } from '@testing-library/react';
import sinon from 'sinon';
import { Globals } from '@cratis/arc';
import { CommandResult } from '@cratis/arc/commands';
import { ArcConfiguration, ArcContext } from '../../ArcContext.js';
import { useCommand } from '../useCommand.js';
import { ConfiguredCommand } from './ConfiguredCommand.js';

for (const [effect, useExecutionEffect] of [['layout', useLayoutEffect], ['passive', useEffect]] as const) {
    describe(`when executing from a child ${effect} effect after command configuration changes`, () => {
        let fetchStub: sinon.SinonStub;
        let view: RenderResult;
        let pending: Promise<CommandResult>;

        beforeEach(async () => {
            fetchStub = sinon.stub(globalThis, 'fetch').resolves({
                status: 200,
                json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString() })
            } as Response);
            const initial: ArcConfiguration = {
                microservice: 'first-service', apiBasePath: '/first', origin: 'https://first.example',
                httpHeadersCallback: () => ({ 'X-Custom': 'first' })
            };
            const next: ArcConfiguration = {
                microservice: 'second-service', apiBasePath: '/second', origin: 'https://second.example',
                httpHeadersCallback: () => ({ 'X-Custom': 'second' })
            };

            function Child({ command }: { command: ConfiguredCommand }) {
                const { origin } = useContext(ArcContext);
                useExecutionEffect(() => { pending = command.execute(); }, [origin]);
                return React.createElement('span', undefined, origin);
            }

            function Consumer() {
                const [command] = useCommand(ConfiguredCommand);
                return React.createElement(Child, { command });
            }

            view = render(React.createElement(ArcContext.Provider, { value: initial }, React.createElement(Consumer)));
            await act(async () => { await pending; });
            fetchStub.resetHistory();

            await act(async () => {
                view.rerender(React.createElement(ArcContext.Provider, { value: next }, React.createElement(Consumer)));
            });
            await act(async () => { await pending; });
        });

        afterEach(() => {
            view.unmount();
            fetchStub.restore();
        });

        it('should execute once after the committed update', () => fetchStub.calledOnce.should.be.true);
        it('should use the new origin and base path', () => String(fetchStub.firstCall.args[0]).should.equal('https://second.example/second/save'));
        it('should use the new microservice', () => fetchStub.firstCall.args[1].headers[Globals.microserviceHttpHeader].should.equal('second-service'));
        it('should use the new headers callback', () => fetchStub.firstCall.args[1].headers['X-Custom'].should.equal('second'));
    });
}
