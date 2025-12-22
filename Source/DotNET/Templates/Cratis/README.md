# CratisApp

A web application built with Cratis Arc and Chronicle.

## Prerequisites

- .NET 9.0 or later
- Docker and Docker Compose (for running Chronicle and Aspire Dashboard)
//#if (EnableFrontend)
- Node.js 20 or later
- Yarn
//#endif

## Getting Started

1. Start the infrastructure:

```bash
docker-compose up -d
```

This will start:
- Chronicle (with MongoDB on port 27017)
- Aspire Dashboard (on port 18888)

//#if (EnableFrontend)
2. Install frontend dependencies:

```bash
yarn install
```

3. Start the frontend development server:

```bash
yarn dev
```

4. Run the application:

```bash
dotnet run
```

The application will be available at:
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Frontend: http://localhost:5173 (Vite dev server)
- Aspire Dashboard: http://localhost:18888

//#else
2. Run the application:

```bash
dotnet run
```

The application will be available at:
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Aspire Dashboard: http://localhost:18888
//#endif

## Project Structure

- `Source/` - All source code (backend and frontend side by side)
  - `Program.cs` - Application entry point
  - `appsettings.json` - Configuration
  - `CratisApp.csproj` - .NET project file
  //#if (EnableFrontend)
  - `index.html` - Frontend entry point
  - `App.tsx`, `main.tsx` - Frontend components
  - `App.css`, `index.css` - Styles
  - `package.json` - Node.js dependencies
  - `vite.config.ts` - Vite configuration
  - `tsconfig.json` - TypeScript configuration
  //#endif
- `docker-compose.yml` - Infrastructure services
- `wwwroot/` - Built frontend files (generated)

## Learn More

- [Cratis Arc Documentation](https://github.com/Cratis/Arc)
- [Chronicle Documentation](https://github.com/Cratis/Chronicle)
