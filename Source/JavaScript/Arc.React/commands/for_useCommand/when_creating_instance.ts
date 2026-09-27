// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { useCommand } from '../useCommand.js';
import { FakeCommand } from './FakeCommand.js';
import { ArcContext, ArcConfiguration } from '../../ArcContext.js';

describe('when creating instance', () => {
    let capturedCommand: FakeCommand | null = null;

    const TestComponent = () => {
        const [command] = useCommand(FakeCommand);
        capturedCommand = command;
        return React.createElement('div', null, 'Test');
    };

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
        httpHeadersCallback: () => ({ 'X-Custom-Header': 'custom-value' })
    };

    render(
        React.createElement(
            ArcContext.Provider,
            { value: config },
            React.createElement(TestComponent)
        )
    );

    it('should set microservice from context', () => capturedCommand!['_microservice'].should.equal('test-microservice'));
    it('should set api base path from context', () => capturedCommand!['_apiBasePath'].should.equal('/api'));
    it('should set origin from context', () => capturedCommand!['_origin'].should.equal('https://example.com'));
    it('should set http headers callback from context', () => {
        const headers = capturedCommand!['_httpHeadersCallback']();
        headers.should.deep.equal({ 'X-Custom-Header': 'custom-value' });
    });
});
