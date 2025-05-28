# Biller

Biller is a modular, multi-layered billing and invoicing platform. It features a .NET-based backend (WebAPI), domain-driven design, PostgreSQL database, and Dockerized deployment, making it suitable for modern cloud-native or on-premises environments.

## Features

- **Modular architecture**: Separation of Application, Domain, Infrastructure, Client, and WebAPI layers.
- **RESTful Web API**: Built with ASP.NET Core, using controllers and middleware for extensibility.
- **PostgreSQL database**: Managed via Docker and migrations (Liquibase).
- **Integrated logging**: Uses Serilog for flexible, structured log management.
- **Swagger UI**: API documentation and exploration out of the box.
- **Docker-based deployment**: Rapid local or cloud deployment with `docker-compose`.
- **Certificate support**: Prepares for HTTPS endpoints with local development certificates.
- **Admin interface**: Includes pgAdmin for managing the PostgreSQL instance.

## Project Structure

- `src/Application` – Business logic, use cases, and application services.
- `src/Client` – External API calls.
- `src/Common` – Shared types and utilities.
- `src/Contracts` – Interfaces and data contracts for API and domain exchange.
- `src/Domain` – Core domain models, aggregates, and logic.
- `src/Infrastructure` – Implementation of persistence, external integrations, etc.
- `src/Validators` – Input validation logic.
- `src/WebAPI` – ASP.NET Core Web API entry point and infrastructure.

## Getting Started

### Prerequisites

- [.NET 6+ SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started)
- [Node.js](https://nodejs.org/) (if developing the client)

### Running with Docker

To run the backend, database, and admin tools with Docker:

```sh
docker-compose up --build
```

This starts:
- PostgreSQL database (`db`)
- pgAdmin web UI (`pgadmin`)
- Web API service (`webapi`)
- Liquibase for database migrations (`liquibase`)

Access:
- API: [https://localhost:8091](https://localhost:8091)
- Swagger UI: [https://localhost:8091/swagger](https://localhost:8091/swagger)
- pgAdmin: [http://localhost:8888](http://localhost:8888)

### Development

To run the backend without Docker:

```sh
cd src/WebAPI
dotnet run
```

To run the client (if present):

```sh
cd src/Client
npm install
npm start
```

## Configuration

- Database connection and credentials are defined in `docker-compose.yml`.
- Certificates for HTTPS are mapped from your local machine (see Docker volumes).
- Environment variables and ASP.NET Core settings can be adjusted in Docker or via `launchSettings.json`.

## Migrations

Database schema is managed by Liquibase. Migration scripts are located in the `migrations` directory and are automatically applied by the `liquibase` container.

## Contributing

Issues and pull requests are welcome! Please see the [LICENSE.txt](LICENSE.txt) for license information.

---

*This README was generated based on repository structure and code conventions. For more specific usage, see the source code and Docker definitions.*
