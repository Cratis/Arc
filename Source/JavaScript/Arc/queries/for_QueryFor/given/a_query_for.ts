// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    TestQueryFor,
    TestEnumerableQueryFor,
    TestQueryForWithoutRequiredParams,
    TestQueryForWithParameterDescriptorValues,
    TestQueryForWithMultipleRequiredParameters
} from './TestQueries';

export class a_query_for {
    query: TestQueryFor;
    enumerableQuery: TestEnumerableQueryFor;
    queryWithoutParams: TestQueryForWithoutRequiredParams;
    queryWithParameterDescriptorValues: TestQueryForWithParameterDescriptorValues;
    queryWithMultipleRequiredParameters: TestQueryForWithMultipleRequiredParameters;

    constructor() {
        this.query = new TestQueryFor();
        this.enumerableQuery = new TestEnumerableQueryFor();
        this.queryWithoutParams = new TestQueryForWithoutRequiredParams();
        this.queryWithParameterDescriptorValues = new TestQueryForWithParameterDescriptorValues();
        this.queryWithMultipleRequiredParameters = new TestQueryForWithMultipleRequiredParameters();
    }
}