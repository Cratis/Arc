// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { SomeCommand } from '../SomeCommand';
import { createFetchHelper } from '../../../helpers/fetchHelper';

export class a_command {
    command: SomeCommand;
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    constructor() {
        this.command = new SomeCommand();
        this.command.route = '/test-route';
        this.command.setOrigin('http://localhost');
        this.command.setApiBasePath('/api');
        this.command.someProperty = 'test-value';
        
        this.fetchHelper = createFetchHelper();
        this.fetchStub = this.fetchHelper.stubFetch();
    }
}
