// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    TestObservableQuery,
    TestEnumerableQuery,
    TestObservableQueryWithParameterDescriptorValues,
    TestObservableQueryWithRouteAndQueryArgs,
    TestObservableQueryWithMultipleRequiredParameters
} from './TestQueries';

export class an_observable_query_for {
    query: TestObservableQuery;
    enumerableQuery: TestEnumerableQuery;
    queryWithParameterDescriptorValues: TestObservableQueryWithParameterDescriptorValues;
    queryWithRouteAndQueryArgs: TestObservableQueryWithRouteAndQueryArgs;
    queryWithMultipleRequiredParameters: TestObservableQueryWithMultipleRequiredParameters;

    constructor() {
        this.query = new TestObservableQuery();
        this.enumerableQuery = new TestEnumerableQuery();
        this.queryWithParameterDescriptorValues = new TestObservableQueryWithParameterDescriptorValues();
        this.queryWithRouteAndQueryArgs = new TestObservableQueryWithRouteAndQueryArgs();
        this.queryWithMultipleRequiredParameters = new TestObservableQueryWithMultipleRequiredParameters();
    }
}