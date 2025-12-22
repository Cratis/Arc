# Cratis Package

This package provides a simplified setup for Cratis Arc applications with Chronicle event sourcing and MongoDB storage.

## Installation

```bash
dotnet add package Cratis
```

## Usage

In your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratis();

var app = builder.Build();
app.UseCratis();

app.Run();
```

This will configure:
- Cratis Arc (commands, queries, validation)
- Chronicle (event sourcing)
- MongoDB (default storage)
- Swagger/OpenAPI documentation

## Configuration

Add the following to your `appsettings.json`:

```json
{
  "Cratis": {
    "Arc": {
    },
    "MongoDB": {
      "Server": "localhost",
      "Port": 27017,
      "Database": "YourAppName"
    }
  }
}
```

## Getting Started

Use the Cratis template to create a new application:

```bash
dotnet new install Cratis.Templates
dotnet new cratis -n MyApp
```

Or with React frontend:

```bash
dotnet new cratis -n MyApp --EnableFrontend true
```

## Learn More

- [Arc Documentation](https://github.com/Cratis/Arc)
- [Chronicle Documentation](https://github.com/Cratis/Chronicle)
