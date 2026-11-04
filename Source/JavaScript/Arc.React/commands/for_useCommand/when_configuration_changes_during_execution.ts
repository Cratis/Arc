// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { Globals } from '@cratis/arc';
import { CommandResult } from '@cratis/arc/commands';
import { ArcConfiguration, ArcContext } from '../../ArcContext.js';
import { useCommand } from '../useCommand.js';
import { ConfiguredCommand } from './ConfiguredCommand.js';

describe('when command configuration changes during execution', () => {
    let fetchStub: sinon.SinonStub;

    beforeEach(async () => {
        let complete!: (response: Response) => void;
        fetchStub = sinon.stub(globalThis, 'fetch');
        fetchStub.onFirstCall().returns(new Promise(resolve => { complete = resolve; }));
        const response = {
            status: 200,
            json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString() })
        } as Response;
        fetchStub.onSecondCall().resolves(response);
        let config: ArcConfiguration = {
            microservice: 'first-service', apiBasePath: '/first', origin: 'https://first.example',
            httpHeadersCallback: () => ({ Authorization: 'first-token' })
        };
        const wrapper = ({ children }: { children: React.ReactNode }) => React.createElement(ArcContext.Provider, { value: config }, children);
        const hook = renderHook(() => useCommand(ConfiguredCommand), { wrapper });
        act(() => hook.result.current[0].setInitialValues({ name: 'accepted' }));
        let pending!: Promise<CommandResult>;
        act(() => { pending = hook.result.current[0].execute(); });
        let token = 'second-token';
        config = {
            microservice: 'second-service', apiBasePath: '/second', origin: 'https://second.example',
            httpHeadersCallback: () => ({ Authorization: token })
        };
        hook.rerender();
        token = 'refreshed-token';
        await act(async () => {
            complete(response);
            await pending;
        });
        await act(async () => { await hook.result.current[0].execute(); });
    });

    afterEach(() => fetchStub.restore());

    it('should keep the in-flight URL', () => String(fetchStub.firstCall.args[0]).should.equal('https://first.example/first/save'));
    it('should keep the in-flight microservice', () => fetchStub.firstCall.args[1].headers[Globals.microserviceHttpHeader].should.equal('first-service'));
    it('should keep the in-flight headers', () => fetchStub.firstCall.args[1].headers.Authorization.should.equal('first-token'));
    it('should use the new URL on the next request', () => String(fetchStub.secondCall.args[0]).should.equal('https://second.example/second/save'));
    it('should use the new microservice on the next request', () => fetchStub.secondCall.args[1].headers[Globals.microserviceHttpHeader].should.equal('second-service'));
    it('should read dynamic headers on the next request', () => fetchStub.secondCall.args[1].headers.Authorization.should.equal('refreshed-token'));
});
