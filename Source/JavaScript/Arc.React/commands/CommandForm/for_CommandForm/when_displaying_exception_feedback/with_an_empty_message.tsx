// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import sinon from 'sinon';
import type { ExceptionDisplayProps } from '../../index';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with an explicitly empty message', given(a_command_form_with_exception_feedback, context => {
    beforeEach(async () => {
        await context.renderForm({ exceptionMessage: '' });
        await act(async () => context.setResult(context.exceptionResult()));
    });

    it('should preserve the empty message in the fallback', () => {
        context.rendered.getByRole('alert').textContent!.should.equal('');
        context.rendered.container.textContent!.should.equal('');
    });
}));

describe('when displaying exception feedback with an empty message and custom renderer', given(a_command_form_with_exception_feedback, context => {
    let display: sinon.SinonSpy;

    beforeEach(async () => {
        display = sinon.spy((props: ExceptionDisplayProps) => <aside role='alert'>{props.message}</aside>);
        await context.renderForm({ exceptionMessage: '', exceptionDisplayComponent: display });
        await act(async () => context.setResult(context.exceptionResult()));
    });

    it('should pass exactly the empty safe message to the custom renderer', () => {
        display.called.should.equal(true);
        display.getCalls().forEach(call => call.args[0].should.deep.equal({ message: '' }));
    });
}));
