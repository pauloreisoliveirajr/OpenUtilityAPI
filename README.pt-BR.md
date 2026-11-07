# OpenUtilityAPI

A OpenUtilityAPI é uma API REST gratuita e auto-hospedável em .NET 10. A versão 0.2.1 oferece 15 utilidades locais e sete consultas de dados públicos. Core e Brazil também podem ser usados diretamente no .NET. Cálculos e validações são locais. As consultas de CEP, localidades, bancos e observações de indicadores precisam de acesso HTTPS ao ViaCEP, IBGE, BrasilAPI e BCB SGS, respectivamente. As consultas dispensam credenciais.

[English](README.md) · [Contratos da API](docs/api-contracts.md) · [Arquitetura](docs/architecture.md) · [Roadmap](docs/roadmap.md)

## Execução local

Clone o repositório e abra um terminal na raiz. Instale o SDK .NET 10, Node.js 24 e pnpm 11.25.0 (`npm install --global pnpm@11.25.0`) e execute:

```sh
pnpm --dir frontend/playground install --frozen-lockfile
pnpm --dir frontend/playground build
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet run --project src/OpenUtilityAPI.Api --launch-profile http
```

Abra a [página web de demonstração](http://localhost:5146/) para testar a API pelo navegador. Siga os passos em [Como usar a página web](#como-usar-a-página-web).

Abra o [Swagger UI](http://localhost:5146/swagger) ou confira o [status da API](http://localhost:5146/health). O documento OpenAPI fica em `/swagger/v1/swagger.json`.

### Visual Studio 2026

Instale a carga de trabalho **ASP.NET and web development** e o SDK .NET 10. Compile o frontend com os comandos acima antes de executar a solução e sempre que alterar a interface. Abra `OpenUtilityAPI.sln`, defina `OpenUtilityAPI.Api` como projeto de inicialização, selecione o perfil `https` e pressione **F5**. A página fica em `https://localhost:7146/` e o Swagger em `https://localhost:7146/swagger`. Se necessário, execute `dotnet dev-certs https --trust` e aceite a confirmação de confiança no certificado.

## Como usar a página web

Inicie a API e abra o endereço correspondente à forma de execução.

| Forma de execução | Página web |
| --- | --- |
| CLI com o perfil `http` | [http://localhost:5146/](http://localhost:5146/) |
| Visual Studio com o perfil `https` | [https://localhost:7146/](https://localhost:7146/) |
| Docker com a porta 8080 mapeada | [http://localhost:8080/](http://localhost:8080/) |

1. Escolha **Português** ou **English** no cabeçalho. A preferência fica salva no navegador.
2. Selecione uma das 22 ferramentas no catálogo, usando a busca ou as categorias. No celular, abra o menu do cabeçalho.
3. Ajuste os valores do formulário ou selecione **Usar exemplo** para restaurar os dados de demonstração.
4. Selecione **Executar chamada** para visualizar o resultado formatado e o status HTTP.
5. Expanda **Detalhes da chamada** para consultar os dados enviados e recebidos pela API. Tabelas longas têm paginação; o JSON mantém a resposta completa.

A interface usa React, TypeScript e Material UI, servidos pelo próprio .NET. Consulte o [guia do frontend](docs/frontend.md) para desenvolvimento e testes. O Docker compila a interface automaticamente e não exige Node.js no ambiente de execução.

A troca de idioma preserva os valores preenchidos e a resposta atual. O idioma do número por extenso é escolhido separadamente no formulário. Use dados de exemplo ao testar documentos pessoais. As chamadas seguem os limites de uso da API.

A página web fica disponível em todos os ambientes. O Swagger UI e o documento OpenAPI ficam disponíveis somente em `Development` e `Homologation` (HML).

## Consumo da API

A página oferece 15 utilidades POST e sete consultas GET. As consultas incluem CEP, UFs, municípios, diretório de instituições, código COMPE, catálogo de indicadores e última observação de um indicador.

```sh
curl http://localhost:5146/api/v1/br/addresses/01001-000
curl http://localhost:5146/api/v1/br/localities/states/DF/municipalities
curl http://localhost:5146/api/v1/br/banks/001
curl http://localhost:5146/api/v1/br/indicators/ipca
```

Falhas nas fontes externas retornam 502 ou 504. Registros não encontrados retornam 404. As consultas bem-sucedidas usam caches limitados em memória, com prazos descritos nos contratos da API. O catálogo de indicadores é local. As observações incluem data de referência e a unidade percentual da fonte. Por exemplo, uma taxa percentual anual e uma taxa percentual diária se referem a períodos diferentes.

Com a API em execução, rode um dos exemplos:

```sh
dotnet run --project examples/CSharp/CSharp.csproj
node examples/JavaScript/compound-interest.mjs
python examples/Python/compound_interest.py
```

Os exemplos recebem a URL base como primeiro argumento. A URL padrão é `http://localhost:5146`. O exemplo JavaScript requer Node.js com fetch integrado. O exemplo Python usa a biblioteca padrão.

## Docker

```sh
docker build -t openutilityapi:0.2.1 .
docker run --rm -p 127.0.0.1:8080:8080 openutilityapi:0.2.1
```

Abra a [página web de demonstração](http://localhost:8080/). O ambiente padrão do contêiner é `Production`. Para habilitar o Swagger em desenvolvimento local ou HML, adicione `-e ASPNETCORE_ENVIRONMENT=Development` ou `-e ASPNETCORE_ENVIRONMENT=Homologation` antes do nome da imagem no comando de execução. O Swagger fica acessível em `/swagger`. A rota `/health` verifica o status da API.

## Configuração

A API aceita chamadas sem autenticação. O limite padrão é de 120 requisições por minuto por IP remoto, em janelas fixas. Ajuste-o com `RateLimit__RequestsPerMinute`. O excesso retorna HTTP 429 com Problem Details e o cabeçalho `Retry-After` quando disponível. Atrás de um proxy reverso, a API recebe por padrão o endereço do proxy.

O acesso de outras origens pelo navegador fica desabilitado por padrão. Configure origens explícitas com `Cors__Origins__0`, `Cors__Origins__1` e assim por diante. O Kestrel limita o corpo da requisição a 64 KiB. A interface Swagger e seu documento JSON ficam disponíveis somente com ASPNETCORE_ENVIRONMENT definido como Development ou Homologation (HML). A página pública em / fica disponível em todos os ambientes. O nome Homologation é explícito; Staging não habilita o Swagger. Configure o acesso de rede à HML de acordo com sua implantação.

## Pacotes NuGet locais

```sh
dotnet pack src/OpenUtilityAPI.Core -c Release -o artifacts/packages
dotnet pack src/OpenUtilityAPI.Brazil -c Release -o artifacts/packages
```

Adicione `artifacts/packages` como origem local do NuGet e referencie `OpenUtilityAPI.Core` ou `OpenUtilityAPI.Brazil` na versão `0.2.1`. Por exemplo, `OpenUtilityAPI.Core.Finance.Interest(1000m, 0.01m, 12, 2, true)` calcula juros compostos diretamente em .NET.

[Contribuição](CONTRIBUTING.md) · [Segurança](SECURITY.md) · [Histórico](CHANGELOG.md) · [Licença MIT](LICENSE)
