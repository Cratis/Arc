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
                CommandValidator: CommandValidator
            };
        case 'queries':
            return {
                Query: Query,
                QueryFor: Query,
                QueryResult: QueryResult,
                QueryResultWithState: QueryResult,
                QueryValidator: QueryValidator,
                Sorting: Sorting,
                SortingActions: SortingActions,
                SortingActionsForQuery: SortingActionsForQuery,
                Paging: Paging
            };
        case 'validation':
            return {
                Validator: Validator,
                ValidationResult: ValidationResult,
                PropertyValidator: PropertyValidator,
                RuleBuilder: RuleBuilder
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

// ValidationResult class
function ValidationResult(propertyName, message, severity) {
    this.propertyName = propertyName || '';
    this.message = message || '';
    this.severity = severity || 0;
    this.members = [propertyName];
    this.state = null;
}

// PropertyValidator class
function PropertyValidator(propertyName) {
    this.propertyName = propertyName;
    this.rules = [];
}

PropertyValidator.prototype.addRule = function(rule) {
    this.rules.push(rule);
};

PropertyValidator.prototype.validate = function(instance) {
    var results = [];
    for (var i = 0; i < this.rules.length; i++) {
        var rule = this.rules[i];
        if (!rule.validate(instance)) {
            results.push(new ValidationResult(this.propertyName, rule.message, 0));
        }
    }
    return results;
};

// RuleBuilder class
function RuleBuilder(propertyValidator, propertyAccessor) {
    this._propertyValidator = propertyValidator;
    this._propertyAccessor = propertyAccessor;
    this._currentRule = null;
}

RuleBuilder.prototype.notEmpty = function() {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            return value !== null && value !== undefined && value !== '';
        },
        message: 'Value cannot be empty'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.emailAddress = function() {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(value);
        },
        message: 'Invalid email address'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.greaterThanOrEqual = function(minValue) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            return value >= minValue;
        },
        message: 'Value must be greater than or equal to ' + minValue
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.minLength = function(minLength) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            return value.length >= minLength;
        },
        message: 'Value must be at least ' + minLength + ' characters'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.length = function(minLength, maxLength) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            return value.length >= minLength && value.length <= maxLength;
        },
        message: 'Value must be between ' + minLength + ' and ' + maxLength + ' characters'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.greaterThan = function(minValue) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            return value > minValue;
        },
        message: 'Value must be greater than ' + minValue
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.lessThan = function(maxValue) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            return value < maxValue;
        },
        message: 'Value must be less than ' + maxValue
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.maxLength = function(maxLength) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            return value.length <= maxLength;
        },
        message: 'Value must be at most ' + maxLength + ' characters'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.lessThanOrEqual = function(maxValue) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            return value <= maxValue;
        },
        message: 'Value must be less than or equal to ' + maxValue
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.matches = function(pattern) {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            var regex = new RegExp(pattern);
            return regex.test(value);
        },
        message: 'Value must match pattern ' + pattern
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.phone = function() {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            // Simple phone validation - digits, spaces, dashes, parens, plus
            var phoneRegex = /^[\d\s\-\(\)\+]+$/;
            return phoneRegex.test(value) && value.replace(/\D/g, '').length >= 7;
        },
        message: 'Invalid phone number'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.url = function() {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            var urlRegex = /^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$/;
            return urlRegex.test(value);
        },
        message: 'Invalid URL'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.creditCard = function() {
    var self = this;
    this._currentRule = {
        validate: function(instance) {
            var value = self._propertyAccessor(instance);
            if (!value) return true; // Skip if empty (use notEmpty to enforce)
            // Luhn algorithm for credit card validation
            var num = value.replace(/\D/g, '');
            if (num.length < 13 || num.length > 19) return false;
            var sum = 0;
            var isEven = false;
            for (var i = num.length - 1; i >= 0; i--) {
                var digit = parseInt(num.charAt(i), 10);
                if (isEven) {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                isEven = !isEven;
            }
            return (sum % 10) === 0;
        },
        message: 'Invalid credit card number'
    };
    this._propertyValidator.addRule(this._currentRule);
    return this;
};

RuleBuilder.prototype.withMessage = function(message) {
    if (this._currentRule) {
        this._currentRule.message = message;
    }
    return this;
};

// Validator base class
function Validator() {
    this._propertyValidators = {};
}

Validator.prototype.ruleFor = function(propertyAccessor) {
    var propertyName = this._getPropertyName(propertyAccessor);
    var propertyValidator = this._propertyValidators[propertyName];
    
    if (!propertyValidator) {
        propertyValidator = new PropertyValidator(propertyName);
        this._propertyValidators[propertyName] = propertyValidator;
    }

    return new RuleBuilder(propertyValidator, propertyAccessor);
};

Validator.prototype.validate = function(instance) {
    var results = [];
    for (var key in this._propertyValidators) {
        var propertyValidator = this._propertyValidators[key];
        var propertyResults = propertyValidator.validate(instance);
        results = results.concat(propertyResults);
    }
    return results;
};

Validator.prototype.isValidFor = function(instance) {
    return this.validate(instance).length === 0;
};

Validator.prototype._getPropertyName = function(propertyAccessor) {
    var propertyNames = [];
    var proxy = new Proxy({}, {
        get: function(target, prop) {
            if (typeof prop === 'string') {
                propertyNames.push(prop);
            }
            return undefined;
        }
    });

    try {
        propertyAccessor(proxy);
    } catch (e) {
        // Ignore errors - we're just capturing the property name
    }

    return propertyNames[0] || 'unknown';
};

// CommandValidator extends Validator
function CommandValidator() {
    Validator.call(this);
}

CommandValidator.prototype = Object.create(Validator.prototype);
CommandValidator.prototype.constructor = CommandValidator;

// QueryValidator extends Validator
function QueryValidator() {
    Validator.call(this);
}

QueryValidator.prototype = Object.create(Validator.prototype);
QueryValidator.prototype.constructor = QueryValidator;

// Base Command class for proxy generation
function Command(responseType, isResponseTypeEnumerable) {
    this.route = '';
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
    
    // Perform client-side validation if validator is present
    if (this.validation && this.validation.validate) {
        var clientValidationErrors = this.validation.validate(this) || [];
        if (clientValidationErrors.length > 0) {
            // Return a failed CommandResult with validation errors
            return Promise.resolve({
                isSuccess: false,
                isValid: false,
                isAuthorized: true,
                hasExceptions: false,
                validationResults: clientValidationErrors,
                exceptionMessages: [],
                exceptionStackTrace: ''
            });
        }
    }
    
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
    var self = this;
    
    // Use args if provided, otherwise use this (the query instance) for validation
    var parametersToValidate = args || this;
    
    // Perform client-side validation if validator is present
    if (this.validation && this.validation.validate) {
        var clientValidationErrors = this.validation.validate(parametersToValidate) || [];
        if (clientValidationErrors.length > 0) {
            // Return a failed QueryResult with validation errors
            return Promise.resolve({
                data: this.defaultValue,
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: clientValidationErrors.map(function(error) {
                    return {
                        severity: error.severity,
                        message: error.message,
                        members: error.members,
                        state: error.state
                    };
                }),
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: {
                    totalItems: 0,
                    totalPages: 0,
                    page: 0,
                    size: 0
                }
            });
        }
    }
    
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
