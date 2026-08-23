# Backend

ASP.NET Core 10 backend using a clean dependency direction:

```text
Domain <- Application <- Persistence / Infrastructure <- API / Worker
```

`Contracts` contains public transport contracts and is kept outside the Domain model. API maps between transport and application/domain models.

Modules are represented consistently by folders across Domain/Application/Persistence/Infrastructure. Do not create cross-module shortcuts; module communication should go through application contracts/domain events.

## Build

```bash
dotnet restore GetCode.sln
dotnet build GetCode.sln -c Release
dotnet test GetCode.sln -c Release --no-build
```

## Database migrations

Migrations are intentionally not generated in the starter. M00 establishes the first schema after module ownership and durable identifiers are finalized. Migrations belong to `GetCode.Persistence` and must be reviewed like code.
