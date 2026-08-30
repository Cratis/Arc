// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render } from '@testing-library/react';
import sinon from 'sinon';
import { ArcContext, type ArcConfiguration } from '../../../ArcContext';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { setCommandFormDevelopmentWarningsForTesting } from '../commandFormRuntime';
import { TestCommand } from '../for_CommandForm/TestCommand';

interface OpaqueInputProps extends WrappedFieldProps<string> {
    testId: string;
}

const OpaqueInputComponent = (props: OpaqueInputProps) => (
    <input
        data-testid={props.testId}
        value={props.value}
        onChange={(event) => props.onChange(event)}
    />
);

const OpaqueInput = asCommandFormField<OpaqueInputProps>(OpaqueInputComponent, {
    defaultValue: '',
    extractValue: (event) => (event as React.ChangeEvent<HTMLInputElement>).target.value,
});

const OpaqueLayout = () => (
    <OpaqueInput<TestCommand>
        value={(command) => command.name}
        currentValue='Opaque seed'
        testId='opaque-field'
    />
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

const arcConfiguration: ArcConfiguration = {
    microservice: 'test-microservice',
    apiBasePath: '/api',
    origin: 'https://example.com',
    httpHeadersCallback: () => ({}),
};

const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <ArcContext.Provider value={arcConfiguration}>{children}</ArcContext.Provider>
);

const FormWithOpaqueField = ({
    capture,
}: {
    capture: (command: TestCommand) => void;
}) => (
    <CommandForm command={TestCommand}>
        <CommandProbe capture={capture} />
        <OpaqueLayout />
    </CommandForm>
);

describe('when an opaque component creates a field in development', () => {
    let consoleWarning: sinon.SinonStub;
    let capturedCommand: TestCommand;

    beforeEach(() => {
        setCommandFormDevelopmentWarningsForTesting(true);
        consoleWarning = sinon.stub(console, 'warn');
    });

    afterEach(() => {
        setCommandFormDevelopmentWarningsForTesting(undefined);
        consoleWarning.restore();
    });

    it('should bind at runtime without warning or duplicate rendering', () => {
        const result = render(
            <FormWithOpaqueField capture={(command) => (capturedCommand = command)} />,
            { wrapper: Wrapper },
        );

        (() => {
            (result.getByTestId('opaque-field') as HTMLInputElement).value.should.equal(
                'Opaque seed',
            );
            capturedCommand.name!.should.equal('Opaque seed');
            fireEvent.change(result.getByTestId('opaque-field'), {
                target: { value: 'Runtime-bound edit' },
            });
            result.rerender(
                <FormWithOpaqueField
                    capture={(command) => (capturedCommand = command)}
                />,
            );
        }).should.not.throw();

        capturedCommand.name!.should.equal('Runtime-bound edit');
        result.queryAllByTestId('opaque-field').should.have.lengthOf(1);
        result.container.querySelectorAll('.w-full').should.have.lengthOf(1);
        consoleWarning.notCalled.should.equal(true);
    });
});

describe('when an opaque component creates a field in production', () => {
    let consoleWarning: sinon.SinonStub;

    beforeEach(() => {
        setCommandFormDevelopmentWarningsForTesting(false);
        consoleWarning = sinon.stub(console, 'warn');
    });

    afterEach(() => {
        setCommandFormDevelopmentWarningsForTesting(undefined);
        consoleWarning.restore();
    });

    it('should bind without warning or throwing', () => {
        let result: ReturnType<typeof render>;
        (() => {
            result = render(<FormWithOpaqueField capture={() => undefined} />, {
                wrapper: Wrapper,
            });
            fireEvent.change(result.getByTestId('opaque-field'), {
                target: { value: 'Production edit' },
            });
        }).should.not.throw();
        consoleWarning.notCalled.should.equal(true);
    });
});
