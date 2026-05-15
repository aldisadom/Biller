[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

## Biller

<!-- PROJECT LOGO -->
<div align="center">
  <a href="https://github.com/aldisadom/Biller">
    <img src="images/logo.png" alt="Logo" width="80" height="80">
  </a>
  <p align="center">
    <a href="/docs/README_en.md">English</a>
  </p>
</div>

Biller is a modular, multi-layered billing and invoicing platform. It features a .NET-based backend (WebAPI), domain-driven design, PostgreSQL database, and Dockerized deployment, making it suitable for modern cloud-native or on-premises environments.

<!-- TABLE OF CONTENTS -->
<details open>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#features">Features</a></li>
    <li><a href="#project-structure">Project Structure</a></li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#running-with-docker">Running with Docker</a></li>
        <li><a href="#development">Development</a></li>
      </ul>
    </li>
    <li><a href="#configuration">Configuration</a></li>
    <li><a href="#migrations">Migrations</a></li>
    <li><a href="#contributing">Contributing</a></li>
  </ol>
</details>

## Features

- **Modular architecture**: Separation of Application, Domain, Infrastructure, Client, and WebAPI layers.
- **RESTful Web API**: Built with ASP.NET Core, using controllers and middleware for extensibility.
- **PostgreSQL database**: Managed via Docker and migrations (Liquibase).
- **Integrated logging**: Uses Serilog for flexible, structured log management.
- **Swagger UI**: API documentation and exploration out of the box.
- **Docker-based deployment**: Rapid local or cloud deployment with `docker-compose`.
- **Certificate support**: Prepares for HTTPS endpoints with local development certificates.
- **Admin interface**: Includes pgAdmin for managing the PostgreSQL instance.

<p align="right">(<a href="#Biller">back to top</a>)</p>

## Project Structure

- `src/Application` – Business logic, use cases, and application services.
- `src/Client` – .NET HTTP clients for external API calls.
- `src/Common` – Shared types and utilities.
- `src/Contracts` – Interfaces and data contracts for API and domain exchange.
- `src/Domain` – Core domain models, aggregates, and logic.
- `src/Infrastructure` – Implementation of persistence, external integrations, etc.
- `src/Validators` – Input validation logic.
- `src/WebAPI` – ASP.NET Core Web API entry point and infrastructure.
- `tests/xUnitTests` – Unit and integration tests.

<p align="right">(<a href="#Biller">back to top</a>)</p>

## Getting Started

### Prerequisites

- [.NET 8+ SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started)

<p align="right">(<a href="#Biller">back to top</a>)</p>

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
- API: [http://localhost:8090](http://localhost:8090)
- Swagger UI: [http://localhost:8090/swagger](http://localhost:8090/swagger)
- pgAdmin: [http://localhost:8888](http://localhost:8888)

<p align="right">(<a href="#Biller">back to top</a>)</p>

### Development

To run the backend without Docker:

```sh
cd src/WebAPI
dotnet run
```

To run the tests:

```sh
dotnet test tests/xUnitTests
```

<p align="right">(<a href="#Biller">back to top</a>)</p>

## Configuration

- Database connection and credentials are defined in `docker-compose.yml`.
- Environment variables and ASP.NET Core settings can be adjusted in Docker or via `launchSettings.json`.

<p align="right">(<a href="#Biller">back to top</a>)</p>

## Migrations

Database schema is managed by Liquibase. Migration scripts are located in the `migrations` directory and are automatically applied by the `liquibase` container.

<p align="right">(<a href="#Biller">back to top</a>)</p>

## Contributing

Issues and pull requests are welcome! Please see the [LICENSE.txt](LICENSE.txt) for license information.

<p align="right">(<a href="#Biller">back to top</a>)</p>

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
<!-- git -->
[contributors-shield]: https://img.shields.io/github/contributors/aldisadom/Biller.svg?style=for-the-badge
[contributors-url]: https://github.com/aldisadom/Biller/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/aldisadom/Biller.svg?style=for-the-badge
[forks-url]: https://github.com/aldisadom/Biller/network/members
[stars-shield]: https://img.shields.io/github/stars/aldisadom/Biller.svg?style=for-the-badge
[stars-url]: https://github.com/aldisadom/Biller/stargazers
[issues-shield]: https://img.shields.io/github/issues/aldisadom/Biller.svg?style=for-the-badge
[issues-url]: https://github.com/aldisadom/Biller/issues
[license-shield]: https://img.shields.io/github/license/aldisadom/Biller.svg?style=for-the-badge
[license-url]: https://github.com/aldisadom/Biller/blob/master/LICENSE.txt

<!-- my links -->
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/aldis-adomavicius/

