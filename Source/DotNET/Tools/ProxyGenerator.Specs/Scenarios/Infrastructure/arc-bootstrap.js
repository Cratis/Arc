// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Arc Runtime Bootstrap for ClearScript V8
// This sets up a CommonJS-like module environment for testing generated proxies

// Node.js process shim (required by TypeScript compiler)
var process = {
    env: {},
    platform: 'darwin',
    version: 'v20.0.0',
    versions: { node: '20.0.0' },
    cwd: function() { return '/'; },
    nextTick: function(fn) { setTimeout(fn, 0); },
    stderr: { write: function() {} },
    stdout: { write: function() {} }
};

// CommonJS module shims (required for transpiled TypeScript)
var module = { exports: {} };
var exports = module.exports;

var __modules = {};
var __moduleCache = {};

// Simple require implementation
function require(modulePath) {
    if (__moduleCache[modulePath]) {
        return __moduleCache[modulePath].exports;
    }

    // Handle Arc package imports
    if (modulePath === '@cratis/arc' || modulePath.startsWith('@cratis/arc/')) {
        var subPath = modulePath === '@cratis/arc' ? 'index' : modulePath.replace('@cratis/arc/', '');
        return __loadArcModule(subPath);
    }

    // Handle Arc.React package imports
    if (modulePath === '@cratis/arc.react' || modulePath.startsWith('@cratis/arc.react/')) {
        var subPath = modulePath === '@cratis/arc.react' ? 'index' : modulePath.replace('@cratis/arc.react/', '');
        return __loadArcReactModule(subPath);
    }

    // Handle Fundamentals imports
    if (modulePath === '@cratis/fundamentals' || modulePath.startsWith('@cratis/fundamentals/')) {
        var subPath = modulePath === '@cratis/fundamentals' ? 'index' : modulePath.replace('@cratis/fundamentals/', '');
        return __loadFundamentalsModule(subPath);
    }

    // Handle relative imports (e.g., './CommandResultData', './ComplexCommandResult')
    // These are typically DTO/data classes that just need empty constructor functions
    if (modulePath.startsWith('./') || modulePath.startsWith('../')) {
        var typeName = modulePath.replace(/^\.\.?\//, '').replace(/\.js$/, '');
        return __createRelativeModule(typeName);
    }

    throw new Error('Module not found: ' + modulePath);
}

// Creates a module for relative imports (typically DTO classes)
function __createRelativeModule(typeName) {
    var module = { exports: {} };
    
    // Create a simple constructor function for the type
    var TypeConstructor = function() {};
    TypeConstructor.prototype = {};
    
    module.exports[typeName] = TypeConstructor;
    
    return module.exports;
}

function __loadArcModule(subPath) {
    var key = '@cratis/arc/' + subPath;
    if (__moduleCache[key]) return __moduleCache[key].exports;

    var module = { exports: __getArcModuleExports(subPath) };
    __moduleCache[key] = module;

    return module.exports;
}

function __loadArcReactModule(subPath) {
    var key = '@cratis/arc.react/' + subPath;
    if (__moduleCache[key]) return __moduleCache[key].exports;

    var module = { exports: __getArcReactModuleExports(subPath) };
    __moduleCache[key] = module;

    return module.exports;
}

function __loadFundamentalsModule(subPath) {
    var key = '@cratis/fundamentals/' + subPath;
    if (__moduleCache[key]) return __moduleCache[key].exports;

    var module = { exports: {} };
    __moduleCache[key] = module;

    return module.exports;
}

// Arc module exports provider
function __getArcModuleExports(subPath) {
    switch(subPath) {
        case 'commands':
            return {
                Command: Command,
                CommandPropertyValidators: function() { this.validators = {}; },
                CommandValidator: function() { this.properties = {}; }
            };
        case 'queries':
            return {
                Query: Query,
                QueryFor: Query,
                QueryResult: QueryResult,
                QueryResultWithState: QueryResult,
                QueryValidator: function() { this.properties = {}; },
                Sorting: Sorting,
                SortingActions: SortingActions,
                SortingActionsForQuery: SortingActionsForQuery,
                Paging: Paging
            };
        case 'validation':
            return {
                Validator: function() { this.validate = function() { return []; }; }
            };
        case 'reflection':
            return {
                PropertyDescriptor: PropertyDescriptor,
                ParameterDescriptor: ParameterDescriptor
            };
        default:
            return {};
    }
}

// Arc.React module exports provider
function __getArcReactModuleExports(subPath) {
    switch(subPath) {
        case 'commands':
            return {
                useCommand: function() { return [null, function() {}]; },
                SetCommandValues: function() {},
                ClearCommandValues: function() {}
            };
        case 'queries':
            return {
                useQuery: function() { return { data: null, isLoading: false }; },
                useQueryWithPaging: function() { return { data: null, isLoading: false }; },
                PerformQuery: function() {},
                SetSorting: function() {},
                SetPage: function() {},
                SetPageSize: function() {}
            };
        default:
            return {};
    }
}

// PropertyDescriptor class
function PropertyDescriptor(name, constructor) {
    this.name = name;
    this.constructor = constructor;
}

// Base Command class for proxy generation
function Command(responseType, isResponseTypeEnumerable) {
    this.route = '';
    this.validation = { properties: {} };
    this.propertyDescriptors = [];
    this._responseType = responseType || Object;
    this._isResponseTypeEnumerable = isResponseTypeEnumerable || false;
    this._initialValues = {};
    this._hasChanges = false;
}

Command.prototype.propertyChanged = function(propertyName) {
    // Track property changes - for testing we just mark as changed
    this._hasChanges = true;
};

Command.prototype.setMicroservice = function(microservice) {
    this._microservice = microservice;
};

Command.prototype.setApiBasePath = function(apiBasePath) {
    this._apiBasePath = apiBasePath;
};

Command.prototype.setOrigin = function(origin) {
    this._origin = origin;
};

Command.prototype.setHttpHeadersCallback = function(callback) {
    this._httpHeadersCallback = callback;
};

Command.prototype.execute = function() {
    var self = this;
    var body = {};
    
    // Gather all properties from the command using propertyDescriptors
    if (this.propertyDescriptors) {
        for (var i = 0; i < this.propertyDescriptors.length; i++) {
            var propName = this.propertyDescriptors[i].name;
            var privateName = '_' + propName;
            if (this[privateName] !== undefined) {
                body[propName] = this[privateName];
            }
        }
    }
    
    return fetch(this.route, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function(response) {
        return response.json();
    });
};

// Base Query class for proxy generation (also serves as QueryFor)
function Query(modelType, isEnumerable) {
    this.route = '';
    this.defaultValue = isEnumerable ? [] : {};
    this.parameterDescriptors = [];
    this.modelType = modelType || Object;
    this.enumerable = isEnumerable || false;
    this.sorting = Sorting.none;
    this.paging = Paging.noPaging;
}

Query.prototype.setMicroservice = function(microservice) {
    this._microservice = microservice;
};

Query.prototype.setApiBasePath = function(apiBasePath) {
    this._apiBasePath = apiBasePath;
};

Query.prototype.setOrigin = function(origin) {
    this._origin = origin;
};

Query.prototype.setHttpHeadersCallback = function(callback) {
    this._httpHeadersCallback = callback;
};

Query.prototype.perform = function(args) {
    var params = [];
    
    // Gather all parameters from the query (from parameterDescriptors)
    if (this.parameterDescriptors) {
        for (var i = 0; i < this.parameterDescriptors.length; i++) {
            var propName = this.parameterDescriptors[i].name;
            var value = args ? args[propName] : this[propName];
            if (value !== undefined && value !== null) {
                params.push(encodeURIComponent(propName) + '=' + encodeURIComponent(value));
            }
        }
    }
    
    var url = this.route;
    if (params.length > 0) {
        url += '?' + params.join('&');
    }
    
    return fetch(url, {
        method: 'GET',
        headers: { 'Accept': 'application/json' }
    }).then(function(response) {
        return response.json();
    });
};

// ParameterDescriptor class
function ParameterDescriptor(name, type, required) {
    this.name = name;
    this.type = type || String;
    this.required = required !== undefined ? required : true;
}

// Sorting class
function Sorting(field, direction) {
    this.field = field || '';
    this.direction = direction || 0;
}
Sorting.none = new Sorting('', 0);
Sorting.prototype.hasSorting = function() { return this.field !== ''; };

// Paging class
function Paging(page, pageSize) {
    this.page = page || 0;
    this.pageSize = pageSize || 0;
}
Paging.noPaging = new Paging(0, 0);
Paging.prototype.hasPaging = function() { return this.pageSize > 0; };

// SortingActions class
function SortingActions(field) {
    this.field = field || '';
}

// SortingActionsForQuery class
function SortingActionsForQuery(field, query) {
    this.field = field || '';
    this.query = query;
}

// QueryResult class
function QueryResult(data, isSuccess, isAuthorized, isValid, hasExceptions, validationResults, exceptionMessages, exceptionStackTrace, paging, sorting) {
    this.data = data;
    this.isSuccess = isSuccess !== undefined ? isSuccess : true;
    this.isAuthorized = isAuthorized !== undefined ? isAuthorized : true;
    this.isValid = isValid !== undefined ? isValid : true;
    this.hasExceptions = hasExceptions !== undefined ? hasExceptions : false;
    this.validationResults = validationResults || [];
    this.exceptionMessages = exceptionMessages || [];
    this.exceptionStackTrace = exceptionStackTrace || '';
    this.paging = paging || { page: 0, size: 0, totalItems: 0, totalPages: 0 };
    this.sorting = sorting || [];
}

// Globals for Arc
var Globals = {
    microservice: '',
    apiBasePath: '',
    microserviceHttpHeader: 'X-Cratis-Microservice'
};

// Mock fetch that will be intercepted by the test bridge
var __fetchHandler = null;

function fetch(url, options) {
    return new Promise(function(resolve, reject) {
        if (__fetchHandler) {
            __fetchHandler(url, options, resolve, reject);
        } else {
            reject(new Error('No fetch handler configured'));
        }
    });
}

// AbortController mock
var AbortController = function() {
    this.signal = {};
};
AbortController.prototype.abort = function() {};

// URLSearchParams
var URLSearchParams = function(init) {
    this._params = {};
    if (init) {
        var pairs = init.toString().split('&');
        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split('=');
            if (pair[0]) this._params[pair[0]] = pair[1] || '';
        }
    }
};
URLSearchParams.prototype.append = function(key, value) {
    this._params[key] = value;
};
URLSearchParams.prototype.toString = function() {
    var parts = [];
    for (var key in this._params) {
        parts.push(encodeURIComponent(key) + '=' + encodeURIComponent(this._params[key]));
    }
    return parts.join('&');
};
URLSearchParams.prototype.get = function(key) {
    return this._params[key];
};

// Headers mock
var Headers = function(init) {
    this._headers = {};
    if (init) {
        for (var key in init) {
            this._headers[key.toLowerCase()] = init[key];
        }
    }
};
Headers.prototype.append = function(key, value) {
    this._headers[key.toLowerCase()] = value;
};
Headers.prototype.get = function(key) {
    return this._headers[key.toLowerCase()];
};
