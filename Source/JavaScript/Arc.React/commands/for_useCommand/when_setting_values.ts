// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import { useCommand } from '../useCommand';
import { FakeCommand, FakeCommandContent } from './FakeCommand';
import { ArcContext, ArcConfiguration } from '../../ArcContext';

describe('when setting command values', () => {
    let capturedCommand: FakeCommand | null = null;
    let renderResult: ReturnType<typeof render>;

    const initialValues: FakeCommandContent = {
        someProperty: 'initial-value',
        anotherProperty: 42
    };

    const TestComponent = () => {
        const [command, setCommandValues] = useCommand(FakeCommand, initialValues);
        capturedCommand = command;

        return React.createElement(
            React.Fragment,
            null,
            React.createElement('button', {
                onClick: () => setCommandValues({ someProperty: null as unknown as string }),
                'data-testid': 'set-null'
            }, 'Set null'),
            React.createElement('button', {
                onClick: () => setCommandValues({ someProperty: undefined }),
                'data-testid': 'set-undefined'
            }, 'Set undefined'),
            React.createElement('button', {
                onClick: () => setCommandValues({ anotherProperty: 24 }),
                'data-testid': 'set-omitted'
            }, 'Set omitted')
        );
    };

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(() => {
        capturedCommand = null;
        renderResult = render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(TestComponent)
            )
        );
    });

    it('should set explicit null values', async () => {
        fireEvent.click(renderResult.getByTestId('set-null'));

        await waitFor(() => {
            expect(capturedCommand!.someProperty).toBeNull();
        });
    });

    it('should set explicit undefined values', async () => {
        fireEvent.click(renderResult.getByTestId('set-undefined'));

        await waitFor(() => {
            expect(capturedCommand!.someProperty).toBeUndefined();
        });
    });

    it('should leave omitted properties unchanged', async () => {
        fireEvent.click(renderResult.getByTestId('set-omitted'));

        await waitFor(() => {
            expect(capturedCommand!.anotherProperty).toBe(24);
        });
        capturedCommand!.someProperty!.should.equal('initial-value');
    });
});
