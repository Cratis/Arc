// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { CommandForm } from '../CommandForm';
import type { CommandFormFieldProps } from '../CommandFormField';
import { markAsCommandFormField } from '../commandFormMarkers';
import { setCommandFormDevelopmentWarningsForTesting } from '../commandFormRuntime';
import { RadioButtonField, RadioGroupField } from '../fields';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface LegacyFieldProps extends CommandFormFieldProps<TestCommand> {
    testId: string;
}

const LegacyField = markAsCommandFormField((props: LegacyFieldProps) => (
    <input data-testid={props.testId} value={String(props.currentValue ?? '')} readOnly />
));

const OpaqueLayout = ({ children }: { children: React.ReactNode }) => (
    <section>{children}</section>
);

const FormWithInvalidFields = () => (
    <CommandForm command={TestCommand}>
        <OpaqueLayout>
            <RadioButtonField
                value={() => 'unbound'}
                setValue='radio-button'
                label='Radio button'
            />
            <RadioGroupField
                value={() => 'unbound'}
                options={[{ value: 'radio-group', label: 'Radio group' }]}
            />
            <LegacyField value={() => 'unbound'} testId='legacy-field' />
        </OpaqueLayout>
    </CommandForm>
);

describe(
    'when runtime and legacy fields have invalid accessors in development',
    given(a_command_form_context, (context) => {
        let consoleWarning: sinon.SinonStub;

        beforeEach(async () => {
            setCommandFormDevelopmentWarningsForTesting(true);
            consoleWarning = sinon.stub(console, 'warn');
            const result = render(<FormWithInvalidFields />, {
                wrapper: context.createWrapper(),
            });
            result.rerender(<FormWithInvalidFields />);
            await waitFor(() => consoleWarning.callCount.should.equal(3));
        });

        afterEach(() => {
            setCommandFormDevelopmentWarningsForTesting(undefined);
            consoleWarning.restore();
        });

        it('should warn once for each mounted field', () => {
            consoleWarning.callCount.should.equal(3);
        });

        it('should identify each field without a duplicate BoundField warning', () => {
            const warnings = consoleWarning
                .getCalls()
                .map((call) => String(call.args[0]));
            warnings
                .some((warning) => warning.includes('RadioButtonFieldComponent'))
                .should.equal(true);
            warnings
                .some((warning) => warning.includes('RadioGroupFieldComponent'))
                .should.equal(true);
            warnings
                .some((warning) => warning.includes("'CommandFormField'"))
                .should.equal(true);
            warnings
                .some((warning) => warning.includes('BoundField'))
                .should.equal(false);
        });
    }),
);

describe(
    'when runtime and legacy fields have invalid accessors in production',
    given(a_command_form_context, (context) => {
        let consoleWarning: sinon.SinonStub;

        beforeEach(() => {
            setCommandFormDevelopmentWarningsForTesting(false);
            consoleWarning = sinon.stub(console, 'warn');
            const result = render(<FormWithInvalidFields />, {
                wrapper: context.createWrapper(),
            });
            result.rerender(<FormWithInvalidFields />);
        });

        afterEach(() => {
            setCommandFormDevelopmentWarningsForTesting(undefined);
            consoleWarning.restore();
        });

        it('should not warn', () => {
            consoleWarning.notCalled.should.equal(true);
        });
    }),
);
