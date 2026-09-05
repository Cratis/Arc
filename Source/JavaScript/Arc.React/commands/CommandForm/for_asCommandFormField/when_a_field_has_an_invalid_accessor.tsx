// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render } from '@testing-library/react';
import sinon from 'sinon';
import { ArcContext, type ArcConfiguration } from '../../../ArcContext';
import { CommandForm } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { setCommandFormDevelopmentWarningsForTesting } from '../commandFormRuntime';
import { TestCommand } from '../for_CommandForm/TestCommand';

interface InvalidInputProps extends WrappedFieldProps<string> {
    testId: string;
}

const InvalidInputComponent = (props: InvalidInputProps) => (
    <input
        data-testid={props.testId}
        value={props.value}
        onChange={(event) => props.onChange(event)}
    />
);

const InvalidInput = asCommandFormField<InvalidInputProps>(InvalidInputComponent, {
    defaultValue: '',
    extractValue: (event) => (event as React.ChangeEvent<HTMLInputElement>).target.value,
});

const MissingAccessorInput = InvalidInput as unknown as React.ComponentType<{
    testId: string;
}>;

const arcConfiguration: ArcConfiguration = {
    microservice: 'test-microservice',
    apiBasePath: '/api',
    origin: 'https://example.com',
    httpHeadersCallback: () => ({}),
};

const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <ArcContext.Provider value={arcConfiguration}>{children}</ArcContext.Provider>
);

const FormWithInvalidFields = () => (
    <CommandForm command={TestCommand}>
        <InvalidInput<TestCommand>
            value={(command) => command as unknown as string}
            testId='invalid-accessor'
        />
        <MissingAccessorInput testId='missing-accessor' />
    </CommandForm>
);

describe('when a field has an invalid accessor in development', () => {
    let consoleWarning: sinon.SinonStub;

    beforeEach(() => {
        setCommandFormDevelopmentWarningsForTesting(true);
        consoleWarning = sinon.stub(console, 'warn');
    });

    afterEach(() => {
        setCommandFormDevelopmentWarningsForTesting(undefined);
        consoleWarning.restore();
    });

    it('should warn once for each mounted invalid field and remain non-throwing', () => {
        const result = render(<FormWithInvalidFields />, { wrapper: Wrapper });

        (() => {
            fireEvent.change(result.getByTestId('invalid-accessor'), {
                target: { value: 'Ignored' },
            });
            fireEvent.change(result.getByTestId('missing-accessor'), {
                target: { value: 'Ignored' },
            });
            result.rerender(<FormWithInvalidFields />);
        }).should.not.throw();

        consoleWarning.callCount.should.equal(2);
        consoleWarning.getCalls().forEach((call) => {
            call.args[0].should.contain('InvalidInputComponent');
            call.args[0].should.contain('could not resolve a command property');
            call.args[0].should.contain('remains unbound');
        });
    });
});

describe('when a field has an invalid accessor in production', () => {
    let consoleWarning: sinon.SinonStub;

    beforeEach(() => {
        setCommandFormDevelopmentWarningsForTesting(false);
        consoleWarning = sinon.stub(console, 'warn');
    });

    afterEach(() => {
        setCommandFormDevelopmentWarningsForTesting(undefined);
        consoleWarning.restore();
    });

    it('should neither warn nor throw', () => {
        (() => {
            const result = render(<FormWithInvalidFields />, { wrapper: Wrapper });
            fireEvent.change(result.getByTestId('invalid-accessor'), {
                target: { value: 'Ignored' },
            });
            fireEvent.change(result.getByTestId('missing-accessor'), {
                target: { value: 'Ignored' },
            });
        }).should.not.throw();

        consoleWarning.notCalled.should.equal(true);
    });
});
