# Commands

The Arc provides comprehensive support for implementing commands in your backend application. Commands represent actions or operations that modify the state of your system and are a fundamental part of CQRS (Command Query Responsibility Segregation) architecture.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Controller based](./controller-based.md) | How to implement commands using controller-based approach. |
| [Model Bound](./model-bound/index.md) | How to work with model-bound commands for simplified parameter handling. |
| [Command Pipeline](./command-pipeline.md) | How to execute commands programmatically using `ICommandPipeline`. |
| [Command Context](./command-context.md) | Understanding CommandContext and how to extend it with custom values for the non-controller-based pipeline. |
| [Command Filters](./command-filters.md) | How to implement command filters for cross-cutting concerns in the non-controller-based pipeline. |
| [Authorization](../authorization.md) | How to use authorization attributes for role-based and policy-based authorization. |
| [Response Value Handlers](./response-value-handlers.md) | How to customize command response handling with value handlers. |
| [Response Examples](./response-examples.md) | Comprehensive examples of different command response patterns. |
| [Validation](./validation.md) | How to implement validation for commands. |
| [Command Validation](./command-validation.md) | How to validate commands without executing them for pre-flight validation. |

> **💡 Frontend Integration**: Automatically generate TypeScript proxies for your commands with the [Proxy Generation](../proxy-generation/) feature.

## Overview

Commands in the Arc are designed to be simple to implement while providing powerful features like automatic validation, response handling, and integration with the overall application architecture. Whether you prefer controller-based approaches or model-bound commands, the framework provides the flexibility to work with your preferred style while maintaining consistency and best practices.

### Key Features

- **Automatic Response Handling**: Command handlers can return any value, which will either be processed by custom response value handlers or automatically become a typed `CommandResult<T>` response
- **Flexible Return Types**: Support for single values, tuples, and OneOf types with intelligent processing
- **Built-in Validation**: Automatic command validation before execution
- **Pre-flight Validation**: Validate commands without executing them using the `validate()` method for early user feedback
- **Response Value Handlers**: Extensible system for processing specific types of return values
- **Controller and Model-Bound Support**: Multiple patterns for implementing commands
- **TypeScript Integration**: Automatic proxy generation for frontend integration
