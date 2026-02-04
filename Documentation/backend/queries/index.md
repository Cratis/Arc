# Queries

The Arc provides comprehensive support for implementing queries in your backend application.
Queries are used for retrieving data and are a key component of CQRS (Command Query Responsibility Segregation) architecture, offering powerful features like observability, validation, and flexible parameter handling.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Controller based](./controller-based/index.md) | How to implement queries using controller-based approach. |
| [Model Bound](./model-bound/index.md) | How to work with model-bound queries for simplified parameter handling. |
| [Query Pipeline](./query-pipeline.md) | Understanding the query pipeline and how queries are processed. |
| [Authorization](../authorization.md) | How to use authorization attributes for role-based and policy-based authorization. |
| [Validation](./validation.md) | How to implement validation for query parameters. |

> **💡 Query Filters**: The query pipeline supports filters for cross-cutting concerns like validation and authorization. See the [Query Pipeline](./query-pipeline.md#query-filters) section for details on built-in filters and creating custom ones.

> **💡 Frontend Integration**: Automatically generate TypeScript proxies for your queries with the [Proxy Generation](../proxy-generation/index.md) feature.

## Overview

Queries in the Arc are designed to be flexible and powerful, supporting both traditional request-response patterns and reactive, observable queries that can provide real-time updates.
The framework handles parameter validation, binding, and processing through a comprehensive pipeline that ensures consistent behavior across your application.
Query arguments and parameter binding are covered within the controller-based and model-bound topics.
