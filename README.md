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

## Backup and restore

`make update` takes a backup first and stops if it fails. Run `make backup` yourself before any
other change that touches data, such as deploying a migration by hand.

```sh
make backup     # saves backup/billio-YYYYMMDD-HHMMSS.dump (UTC), keeps the newest 14
make restore    # lists the backups in backup/, asks which one, then restores it into Data
```

A restore goes into a temp database first, so a broken backup leaves `Data` as it was. Only when
that succeeds does it stop `webapi`, back up the current `Data` to
`backup/billio-YYYYMMDD-HHMMSS-before-restore.dump` (so picking the wrong file can be undone), swap
the restored database in, run Liquibase and start `webapi` again. If Liquibase fails, `webapi` is
left stopped. The backup includes
Liquibase's `databasechangelog` table, so after restoring an older backup only the newer migrations
are applied.

**Check a backup** without touching the live database: restore it into a scratch database, compare
its row counts with the live ones, then drop it:

```sh
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"Data_restore_test\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d Data_restore_test --no-owner --exit-on-error < backup/billio-....dump
docker exec billio-db psql -U postgres -d Data_restore_test -c "SELECT count(*) FROM invoices;"
docker exec billio-db psql -U postgres -d Data -c "SELECT count(*) FROM invoices;"
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE \"Data_restore_test\";"
```

**Without the scripts:**

```sh
# backup
docker exec billio-db pg_dump -U postgres -d Data -Fc > backup/billio.dump

# restore: into a temp database first; go on only if pg_restore succeeds
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS \"Data_restore_tmp\" WITH (FORCE);"
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"Data_restore_tmp\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d Data_restore_tmp --no-owner --exit-on-error < backup/billio.dump
docker stop billio-webapi
# back up the current Data; go on only if both commands succeed
undo=backup/billio-$(date -u +%Y%m%d-%H%M%S)-before-restore.dump
docker exec billio-db pg_dump -U postgres -d Data -Fc > "$undo"
docker exec -i billio-db pg_restore --list < "$undo" | grep -m1 "TABLE DATA"
# swap, migrate, and drop the old database only once Liquibase succeeded
docker exec billio-db psql -U postgres -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'Data'; ALTER DATABASE \"Data\" RENAME TO \"Data_restore_old\"; ALTER DATABASE \"Data_restore_tmp\" RENAME TO \"Data\";"
docker start -a billio-liquibase
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE \"Data_restore_old\" WITH (FORCE);"
docker start billio-webapi
```

Keep a copy of the backups off the PC (USB drive or cloud folder). A backup on the same disk does
not survive a disk failure.

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

