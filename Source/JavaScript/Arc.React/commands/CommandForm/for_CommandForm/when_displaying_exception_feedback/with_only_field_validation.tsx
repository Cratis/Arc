// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import sinon from 'sinon';
import { InputTextField, type ErrorDisplayProps, type ExceptionDisplayProps } from '../../index';
import { TestCommand } from '../TestCommand';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with only field validation', given(a_command_form_with_exception_feedback, context => {
    let fieldDisplay: sinon.SinonSpy;
    let exceptionDisplay: sinon.SinonSpy;

    beforeEach(async () => {
        fieldDisplay = sinon.spy((props: ErrorDisplayProps) => <span>{props.errors.join(', ')}</span>);
        exceptionDisplay = sinon.spy((props: ExceptionDisplayProps) => <aside role='alert'>{props.message}</aside>);
        await context.renderForm({
            errorDisplayComponent: fieldDisplay,
            exceptionDisplayComponent: exceptionDisplay,
            children: <InputTextField<TestCommand> value={command => command.name} />
        });
        fieldDisplay.resetHistory();
        const result = CommandResult.validationFailed([
            new ValidationResult(ValidationResultSeverity.Error, 'Please enter your name', ['name'], {})
        ]);
        await act(async () => context.setResult(result));
    });

    it('should retain the field validation message', () => {
        context.rendered.getByText('Please enter your name').textContent!.should.equal('Please enter your name');
    });
    it('should call the field renderer only for the named field', () => {
        fieldDisplay.called.should.equal(true);
        fieldDisplay.getCalls().forEach(call => {
            call.args[0].fieldName.should.equal('name');
            call.args[0].errors.should.deep.equal(['Please enter your name']);
        });
    });
    it('should not invoke the exception renderer or show an exception fallback', () => {
        exceptionDisplay.called.should.equal(false);
        (context.rendered.queryByRole('alert') === null).should.equal(true);
        context.rendered.container.textContent!.should.not.contain(context.defaultMessage);
    });
}));
