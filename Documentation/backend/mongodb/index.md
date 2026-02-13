# MongoDB

Welcome to the MongoDB documentation for Cratis Applications. This section covers all aspects of MongoDB integration, from basic setup to advanced configuration and mapping features.

MongoDB support in Cratis Applications provides a comprehensive set of features designed to make working with MongoDB in .NET applications seamless and productive. The framework sets up sensible defaults while allowing for extensive customization when needed.

## Key Features

- **Easy Setup**: Simple configuration with sane defaults
- **Custom Serializers**: Built-in serializers for common .NET types
- **Concept Support**: Automatic serialization support for Cratis Concepts
- **Naming Policies**: Flexible naming conventions for collections and properties
- **Class Mapping**: Automatic discovery and registration of custom mappings
- **Convention Packs**: Extensible convention system with filtering capabilities

## Topics

- [**Getting Started**](getting-started.md) - Basic setup and configuration
- [**Serializers**](serializers.md) - Built-in serializers for common types
- [**Concepts**](concepts.md) - Working with Cratis Concepts in MongoDB
- [**Naming Policies**](naming-policies.md) - Configuring naming conventions
- [**Class Mapping**](class-mapping.md) - Custom BSON class mapping with automatic discovery
- [**Convention Packs**](convention-packs.md) - Advanced convention system and filtering
- [**Tenancy**](tenancy.md) - MongoDB tenant database naming strategies

## Quick Start

To get started with MongoDB in your application:

```csharp
var builder = WebApplication.CreateBuilder(args)
    .UseCratisArc();

builder.UseCratisMongoDB();
```

This single line configures your application with MongoDB support, including all default serializers, conventions, and mappings.
