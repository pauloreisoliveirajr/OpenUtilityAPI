# Contributing

Use the .NET 10 SDK and follow the local setup in the [README](README.md). Discuss scope changes in an issue before opening a pull request. Keep changes focused and write code, technical comments and API documentation in English.

Run `dotnet restore`, `dotnet build -c Release --no-restore` and `dotnet test -c Release --no-build` before submitting. For contract changes, update Swagger metadata, the [API contracts](docs/api-contracts.md) and relevant unit and integration tests. Use synthetic or published test vectors and keep credentials and personal data out of reports and fixtures.
