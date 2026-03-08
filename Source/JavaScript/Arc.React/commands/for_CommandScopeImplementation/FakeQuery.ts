// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { IQueryFor, QueryResult, Paging, Sorting } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class FakeQuery implements IQueryFor<any, any> {
    route = '';
    requiredRequestParameters: string[] = [];
    defaultValue: any = null;
    parameterDescriptors: ParameterDescriptor[] = [];
    parameters: any;
    sorting = Sorting.none;
    paging = Paging.noPaging;

    perform: sinon.SinonSpy;
    setApiBasePath: sinon.SinonStub;
    setOrigin: sinon.SinonStub;
    setHttpHeadersCallback: sinon.SinonStub;
    setMicroservice: sinon.SinonStub;

    constructor() {
        this.perform = sinon.fake(() => {
            return new Promise<QueryResult<any>>(resolve => {
                resolve(QueryResult.noSuccess);
            });
        });
        this.setApiBasePath = sinon.stub();
        this.setOrigin = sinon.stub();
        this.setHttpHeadersCallback = sinon.stub();
        this.setMicroservice = sinon.stub();
    }
}
