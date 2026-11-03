// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { Globals } from '@cratis/arc';
import { ArcConfiguration, ArcContext } from '../../ArcContext.js';
import { useCommand } from '../useCommand.js';
import { ConfiguredCommand } from './ConfiguredCommand.js';

for (const [configuration, isSuccess] of [
    ['replacement', true], ['removed optional values', true], ['replacement', false]
] as const) {
    describe(`when command configuration changes with ${configuration} and success is ${isSuccess}`, () => {
        let fetchStub: sinon.SinonStub;
        let initialCommand: ConfiguredCommand;
        let currentCommand: ConfiguredCommand;
        let initialSetValues: unknown;
        let currentSetValues: unknown;
        let hasChanges: boolean;
        let result: CommandResult;
        const initial: ArcConfiguration = {
            microservice: 'first-service', apiBasePath: '/first', origin: 'https://first.example',
            httpHeadersCallback: () => ({ 'X-Custom': 'first' })
        };
        const next: ArcConfiguration = configuration === 'replacement' ? {
            microservice: 'second-service', apiBasePath: '/second', origin: 'https://second.example',
            httpHeadersCallback: () => ({ 'X-Custom': 'second' })
        } : { microservice: '' };

        beforeEach(async () => {
            fetchStub = sinon.stub(globalThis, 'fetch').resolves({
                status: 200,
                json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString(), isSuccess })
            } as Response);
            let config = initial;
            const wrapper = ({ children }: { children: React.ReactNode }) => React.createElement(ArcContext.Provider, { value: config }, children);
            const hook = renderHook(() => useCommand(ConfiguredCommand), { wrapper });
            initialCommand = hook.result.current[0];
            initialSetValues = hook.result.current[1];
            act(() => {
                initialCommand.setInitialValues({ name: 'accepted' });
                initialCommand.name = 'unsaved';
                initialCommand.propertyChanged('name');
            });
            config = next;
            hook.rerender();
            currentCommand = hook.result.current[0];
            currentSetValues = hook.result.current[1];
            hasChanges = currentCommand.hasChanges;
            await act(async () => { result = await currentCommand.execute(); });
        });

        afterEach(() => fetchStub.restore());

        it('should retain the command instance', () => currentCommand.should.equal(initialCommand));
        it('should retain the set values callback', () => currentSetValues!.should.equal(initialSetValues));
        it('should retain unsaved content', () => currentCommand.name.should.equal('unsaved'));
        it('should retain dirty state on rerender', () => hasChanges.should.be.true);
        it('should return the server outcome', () => result.isSuccess.should.equal(isSuccess));
        it('should retain dirty state after a rejection', () => currentCommand.hasChanges.should.equal(!isSuccess));
        it('should retain the accepted baseline', () => {
            currentCommand.name = 'later edit';
            currentCommand.revertChanges();
            currentCommand.name.should.equal(isSuccess ? 'unsaved' : 'accepted');
        });
        it('should use the current URL', () => {
            const expectedOrigin = next.origin ?? document.location.origin;
            String(fetchStub.firstCall.args[0]).should.equal(`${expectedOrigin}${next.apiBasePath ?? ''}/save`);
        });
        it('should use only the current headers', () => {
            const headers = fetchStub.firstCall.args[1].headers;
            (headers['X-Custom'] === (configuration === 'replacement' ? 'second' : undefined)).should.be.true;
            (headers[Globals.microserviceHttpHeader] === (configuration === 'replacement' ? 'second-service' : undefined)).should.be.true;
        });
    });
}
