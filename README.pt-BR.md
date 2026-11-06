# OpenUtilityAPI

A OpenUtilityAPI é uma API REST gratuita e auto-hospedável em .NET 10. A versão 0.1.0 oferece 15 utilidades de documentos brasileiros, finanças, matemática, conversões, datas e texto. As bibliotecas Core e Brazil também podem ser usadas diretamente no .NET. A API dispensa credenciais e serviços externos.

[English](README.md) · [Contratos da API](docs/api-contracts.md) · [Arquitetura](docs/architecture.md) · [Roadmap](docs/roadmap.md)

## Execução local

Clone o repositório e abra um terminal na raiz. Instale o SDK .NET 10 e execute:

```sh
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet run --project src/OpenUtilityAPI.Api --launch-profile http
```

Abra a [página pública de demonstração](http://localhost:5146/) para testar as 15 ferramentas com formulários e resultados em português ou inglês. Use o seletor no cabeçalho; a preferência fica salva neste navegador. A troca de idioma preserva os valores e as respostas. O idioma do número por extenso é escolhido separadamente no formulário. Expanda o painel JSON para consultar a requisição e a resposta. As chamadas usam a mesma API e seus limites de uso.

Abra o [Swagger UI](http://localhost:5146/swagger) ou confira o [status da API](http://localhost:5146/health). O documento OpenAPI fica em `/swagger/v1/swagger.json`.

### Visual Studio 2026

Instale a carga de trabalho **ASP.NET and web development** e o SDK .NET 10. Abra `OpenUtilityAPI.sln`, defina `OpenUtilityAPI.Api` como projeto de inicialização, selecione o perfil `https` e pressione **F5**. O Swagger abre em `https://localhost:7146/swagger`. Se necessário, execute `dotnet dev-certs https --trust` e aceite a confirmação de confiança no certificado.

## Consumo da API

Com a API em execução, rode um dos exemplos:

```sh
dotnet run --project examples/CSharp/CSharp.csproj
node examples/JavaScript/compound-interest.mjs
python examples/Python/compound_interest.py
```

Os exemplos recebem a URL base como primeiro argumento. A URL padrão é `http://localhost:5146`. O exemplo JavaScript requer Node.js com fetch integrado. O exemplo Python usa a biblioteca padrão.

## Docker

```sh
docker build -t openutilityapi:0.1.0 .
docker run --rm -p 127.0.0.1:8080:8080 -e ASPNETCORE_ENVIRONMENT=Development openutilityapi:0.1.0
```

Abra `http://localhost:8080/swagger`. A opção de ambiente habilita o Swagger para desenvolvimento local. O ambiente padrão do contêiner é Production. A rota `/health` verifica o status da API.

## Configuração

A API aceita chamadas sem autenticação. O limite padrão é de 120 requisições por minuto por IP remoto, em janelas fixas. Ajuste-o com `RateLimit__RequestsPerMinute`. O excesso retorna HTTP 429 com Problem Details e o cabeçalho `Retry-After` quando disponível. Atrás de um proxy reverso, a API recebe por padrão o endereço do proxy.

O acesso de outras origens pelo navegador fica desabilitado por padrão. Configure origens explícitas com `Cors__Origins__0`, `Cors__Origins__1` e assim por diante. O Kestrel limita o corpo da requisição a 64 KiB. A interface Swagger e seu documento JSON ficam disponíveis somente com ASPNETCORE_ENVIRONMENT definido como Development ou Homologation (HML). A página pública em / fica disponível em todos os ambientes. O nome Homologation é explícito; Staging não habilita o Swagger. Configure o acesso de rede à HML de acordo com sua implantação.

## Pacotes NuGet locais

```sh
dotnet pack src/OpenUtilityAPI.Core -c Release -o artifacts/packages
dotnet pack src/OpenUtilityAPI.Brazil -c Release -o artifacts/packages
```

Adicione `artifacts/packages` como origem local do NuGet e referencie `OpenUtilityAPI.Core` ou `OpenUtilityAPI.Brazil` na versão `0.1.0`. Por exemplo, `OpenUtilityAPI.Core.Finance.Interest(1000m, 0.01m, 12, 2, true)` calcula juros compostos diretamente em .NET.

[Contribuição](CONTRIBUTING.md) · [Segurança](SECURITY.md) · [Histórico](CHANGELOG.md) · [Licença MIT](LICENSE)
