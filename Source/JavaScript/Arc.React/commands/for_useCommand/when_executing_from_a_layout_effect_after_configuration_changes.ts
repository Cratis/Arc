// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useContext, useLayoutEffect } from 'react';
import { act, renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { Globals } from '@cratis/arc';
import { CommandResult } from '@cratis/arc/commands';
import { ArcConfiguration, ArcContext } from '../../ArcContext.js';
import { useCommand } from '../useCommand.js';
import { ConfiguredCommand } from './ConfiguredCommand.js';

describe('when executing from a layout effect after command configuration changes', () => {
    let fetchStub: sinon.SinonStub;
    let pending: Promise<CommandResult>;

    beforeEach(async () => {
        fetchStub = sinon.stub(globalThis, 'fetch').resolves({
            status: 200,
            json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString() })
        } as Response);
        let config: ArcConfiguration = {
            microservice: 'first-service', apiBasePath: '/first', origin: 'https://first.example',
            httpHeadersCallback: () => ({ 'X-Custom': 'first' })
        };
        const wrapper = ({ children }: { children: React.ReactNode }) => React.createElement(ArcContext.Provider, { value: config }, children);
        const hook = renderHook(() => {
            const { origin } = useContext(ArcContext);
            const [command] = useCommand(ConfiguredCommand);
            useLayoutEffect(() => { pending = command.execute(); }, [origin]);
        }, { wrapper });
        await act(async () => { await pending; });
        fetchStub.resetHistory();

        config = {
            microservice: 'second-service', apiBasePath: '/second', origin: 'https://second.example',
            httpHeadersCallback: () => ({ 'X-Custom': 'second' })
        };
        await act(async () => {
            hook.rerender();
        });
        await act(async () => { await pending; });
    });

    afterEach(() => fetchStub.restore());

    it('should execute once after the committed update', () => fetchStub.calledOnce.should.be.true);
    it('should use the new origin and base path', () => String(fetchStub.firstCall.args[0]).should.equal('https://second.example/second/save'));
    it('should use the new microservice', () => fetchStub.firstCall.args[1].headers[Globals.microserviceHttpHeader].should.equal('second-service'));
    it('should use the new headers callback', () => fetchStub.firstCall.args[1].headers['X-Custom'].should.equal('second'));
});
