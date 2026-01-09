// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { useCommand } from '../useCommand';
import { FakeCommand, FakeCommandContent } from './FakeCommand';
import { ArcContext, ArcConfiguration } from '../../ArcContext';

describe('when creating instance with initial values', () => {
    let capturedCommand: FakeCommand | null = null;

    const initialValues: FakeCommandContent = {
        someProperty: 'initial-value',
        anotherProperty: 42
    };

    const TestComponent = () => {
        const [command] = useCommand(FakeCommand, initialValues);
        capturedCommand = command;
        return React.createElement('div', null, 'Test');
    };

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    render(
        React.createElement(
            ArcContext.Provider,
            { value: config },
            React.createElement(TestComponent)
        )
    );

    it('should set someProperty from initial values', () => capturedCommand!.someProperty!.should.equal('initial-value'));
    it('should set anotherProperty from initial values', () => capturedCommand!.anotherProperty!.should.equal(42));
});
