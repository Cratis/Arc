// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import sinon from 'sinon';
import type { ExceptionDisplayProps } from '../../index';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with errors hidden', given(a_command_form_with_exception_feedback, context => {
    let display: sinon.SinonSpy;

    beforeEach(async () => {
        display = sinon.spy((props: ExceptionDisplayProps) => <div role='alert'>{props.message}</div>);
        await context.renderForm({ showErrors: false, exceptionDisplayComponent: display });
        await act(async () => context.setResult(context.exceptionResult()));
    });

    it('should not invoke the custom exception renderer', () => display.called.should.equal(false));
    it('should not display exception feedback or private diagnostics', () => {
        context.rendered.container.textContent!.should.equal('');
        (context.rendered.queryByRole('alert') === null).should.equal(true);
        context.rendered.container.innerHTML.should.not.contain(context.privateMessage);
        context.rendered.container.innerHTML.should.not.contain(context.privateStack);
    });
}));

describe('when displaying exception feedback with the default renderer and errors hidden', given(a_command_form_with_exception_feedback, context => {
    beforeEach(async () => {
        await context.renderForm({ showErrors: false });
        await act(async () => context.setResult(context.exceptionResult()));
    });

    it('should not display the fallback or private diagnostics', () => {
        context.rendered.container.textContent!.should.equal('');
        (context.rendered.queryByRole('alert') === null).should.equal(true);
        context.rendered.container.innerHTML.should.not.contain(context.privateMessage);
        context.rendered.container.innerHTML.should.not.contain(context.privateStack);
    });
}));
