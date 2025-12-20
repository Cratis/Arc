// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult, ICommand } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

/* eslint-disable */

export class FakeCommand implements ICommand {
    route = '';
    propertyDescriptors: PropertyDescriptor[] = [];
    private _hasChanges: boolean;

    execute: sinon.SinonSpy;
    validate: sinon.SinonSpy;
    clear: sinon.SinonStub;
    setApiBasePath: sinon.SinonStub;
    setOrigin: sinon.SinonStub;
    setHttpHeadersCallback: sinon.SinonStub;
    setInitialValues: sinon.SinonStub;
    propertyChanged: sinon.SinonStub;
    onPropertyChanged: sinon.SinonStub;
    revertChanges: sinon.SinonSpy;
    setInitialValuesFromCurrentValues: sinon.SinonStub;

    constructor(hasChanges: boolean) {
        this._hasChanges = hasChanges;
        this.execute = sinon.fake(() => {
            this._hasChanges = false;
            return new Promise<CommandResult>(resolve => {
                resolve(CommandResult.empty);
            });
        });
        this.validate = sinon.fake(() => {
            return new Promise<CommandResult>(resolve => {
                resolve(CommandResult.empty);
            });
        });
        this.clear = sinon.stub();
        this.setApiBasePath = sinon.stub();
        this.setOrigin = sinon.stub();
        this.setHttpHeadersCallback = sinon.stub();
        this.setInitialValues = sinon.stub();
        this.propertyChanged = sinon.stub();
        this.onPropertyChanged = sinon.stub();
        this.setInitialValuesFromCurrentValues = sinon.stub();
        this.revertChanges = sinon.fake(() => {
            this._hasChanges = false;
        });
    }

    get hasChanges() {
        return this._hasChanges;
    }
}
