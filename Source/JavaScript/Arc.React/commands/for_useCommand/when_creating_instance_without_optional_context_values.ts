// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { useCommand } from '../useCommand.js';
import { FakeCommand } from './FakeCommand.js';
import { ArcContext, ArcConfiguration } from '../../ArcContext.js';

describe('when creating instance without optional context values', () => {
    let capturedCommand: FakeCommand | null = null;

    const TestComponent = () => {
        const [command] = useCommand(FakeCommand);
        capturedCommand = command;
        return React.createElement('div', null, 'Test');
    };

    const config: ArcConfiguration = {
        microservice: 'test-microservice'
    };

    render(
        React.createElement(
            ArcContext.Provider,
            { value: config },
            React.createElement(TestComponent)
        )
    );

    it('should set api base path to empty string', () => capturedCommand!['_apiBasePath'].should.equal(''));
    it('should set origin to empty string', () => capturedCommand!['_origin'].should.equal(''));
    it('should set http headers callback to return empty object', () => {
        const headers = capturedCommand!['_httpHeadersCallback']();
        headers.should.deep.equal({});
    });
});
