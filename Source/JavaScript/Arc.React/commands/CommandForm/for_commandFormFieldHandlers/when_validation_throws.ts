// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { createFieldBlurHandler, createFieldValueChangeHandler, type CommandFormFieldHandlerScope } from '../commandFormFieldHandlers.js';

const issue = 7;

const scopeThatThrows = (validateOn: 'blur' | 'change') => {
    const failSilentValidation = sinon.spy();
    const beginSilentValidation = sinon.stub().returns(issue);
    const commandInstance = {
        validateClientSide: () => { throw new Error('validator exploded'); },
    };
    const scope = {
        context: {
            commandInstance,
            validateOn,
            autoServerValidate: false,
            setCommandValues: () => { },
            setCustomFieldError: () => { },
            beginSilentValidation,
            setSilentValidationResult: () => true,
            failSilentValidation,
        },
        propertyName: 'name',
        fieldProps: {},
        currentValue: 'a',
    } as unknown as CommandFormFieldHandlerScope;
    return { scope, failSilentValidation, beginSilentValidation };
};

describe('when the validation after a value change throws', () => {
    const { scope, failSilentValidation, beginSilentValidation } = scopeThatThrows('change');
    let thrown: unknown;

    beforeEach(async () => {
        try {
            await createFieldValueChangeHandler(scope)('b');
        } catch (error) {
            thrown = error;
        }
    });

    it('should claim a token for the client channel', () => beginSilentValidation.calledWith('client').should.be.true);

    it('should report the validation as failed under its token', () => failSilentValidation.calledOnceWith(issue).should.be.true);

    it('should not swallow the failure', () => (thrown as Error).message.should.equal('validator exploded'));
});

describe('when the validation on blur throws', () => {
    const { scope, failSilentValidation } = scopeThatThrows('blur');
    let thrown: unknown;

    beforeEach(async () => {
        try {
            await createFieldBlurHandler(scope)();
        } catch (error) {
            thrown = error;
        }
    });

    it('should report the validation as failed under its token', () => failSilentValidation.calledOnceWith(issue).should.be.true);

    it('should not swallow the failure', () => (thrown as Error).message.should.equal('validator exploded'));
});
