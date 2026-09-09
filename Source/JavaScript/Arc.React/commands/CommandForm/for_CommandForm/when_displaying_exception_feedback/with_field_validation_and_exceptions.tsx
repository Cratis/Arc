// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import sinon from 'sinon';
import { InputTextField, type ErrorDisplayProps } from '../../index';
import type { TestCommand } from '../TestCommand';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with field validation and exceptions', given(a_command_form_with_exception_feedback, context => {
    let fieldDisplay: sinon.SinonSpy;

    beforeEach(async () => {
        fieldDisplay = sinon.spy((props: ErrorDisplayProps) => <span>{props.errors.join(', ')}</span>);
        await context.renderForm({
            errorDisplayComponent: fieldDisplay,
            children: <InputTextField<TestCommand> value={command => command.name} />
        });
        fieldDisplay.resetHistory();
        const result = context.exceptionResult(true, [context.privateMessage], [
            new ValidationResult(ValidationResultSeverity.Error, 'Please enter your name', ['name'], {})
        ]);
        await act(async () => context.setResult(result));
    });

    it('should show safe exception feedback alongside the unchanged field message', () => {
        context.rendered.getByRole('alert').textContent!.should.equal(context.defaultMessage);
        context.rendered.getByText('Please enter your name').textContent!.should.equal('Please enter your name');
    });
    it('should never call the field renderer for a form level exception', () => {
        fieldDisplay.called.should.equal(true);
        fieldDisplay.getCalls().forEach(call => {
            call.args[0].fieldName.should.equal('name');
            call.args[0].errors.should.deep.equal(['Please enter your name']);
        });
    });
    it('should not put private diagnostics or the old heading in the DOM', () => {
        const markup = context.rendered.container.innerHTML;
        markup.should.not.contain(context.privateMessage);
        markup.should.not.contain(context.privateStack);
        markup.should.not.contain('The server responded with');
    });
}));
