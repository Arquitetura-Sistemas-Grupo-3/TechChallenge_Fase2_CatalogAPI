
# TechChallenge - Catálogo de Jogos (CatalogAPI)

Microserviço responsável pelo catálogo de jogos e início do fluxo de compra.

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- RabbitMQ
- JWT Bearer Authentication
- Swagger

## Pré-requisitos

Antes de executar, garanta que a máquina tenha:

- .NET SDK 8
- SQL Server LocalDB (ou SQL Server configurado na connection string)
- RabbitMQ em execução

## Configuração

### 1) Clonar o repositório

```bash
git clone https://github.com/Arquitetura-Sistemas-Grupo-3/TechChallenge_Fase2_CatalogAPI.git
cd TechChallenge_Fase2_CatalogAPI
```

### 2) Ajustar `appsettings.json`

Arquivo: `WebAPI/appsettings.json`

Pontos importantes:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `RabbitMQ:Host`, `RabbitMQ:User`, `RabbitMQ:Password`

> O token JWT gerado no microserviço de usuários precisa usar o mesmo `Issuer` e a mesma `Key` configurados aqui, caso contrário a autenticação será recusada.

### 3) Subir RabbitMQ (opção rápida com Docker)

```bash
docker run -d --hostname rabbit-local --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Painel (opcional): `http://localhost:15672`  
Usuário/senha padrão: `guest` / `guest`

## Executando a aplicação

Na raiz do projeto:

```bash
dotnet restore
dotnet run --project WebAPI/WebAPI.csproj
```

A API iniciará (por padrão) em:

- `https://localhost:7057`
- `http://localhost:5130`

Swagger:

- `https://localhost:7057/swagger`
- `http://localhost:5130/swagger`

## Como obter o token no microserviço de usuários

1. Execute o microserviço de usuários.
2. Faça login no endpoint de autenticação dele (ex.: `/auth/login`, conforme implementação desse serviço).
3. Copie o JWT retornado.
4. No Swagger desta API, clique em **Authorize** e informe:

```text
Bearer SEU_TOKEN_AQUI
```

## Endpoints principais

Base: `/Jogo`

- `GET /Jogo` (autenticado)
- `GET /Jogo/{id}` (autenticado)
- `POST /Jogo` (**Admin**)
- `PUT /Jogo/{id}` (**Admin**)
- `DELETE /Jogo/{id}` (**Admin**)
- `POST /Jogo/{id}/comprar` (autenticado)

### Exemplo de payload para criar/atualizar jogo

```json
{
  "nome": "Elden Ring",
  "preco": 199.9
}
```

## Fluxo de compra (resumo)

Ao chamar `POST /Jogo/{id}/comprar`:

1. A API cria um pedido com status pendente.
2. Publica evento na fila `order-placed`.
3. Aguarda retorno de pagamento na fila `catalog-payment-processed`.
4. Com pagamento aprovado, o jogo é adicionado à biblioteca do usuário.

## Filas utilizadas

Configuradas em `WebAPI/appsettings.json`:

- `RabbitMQ:OrderPlacedQueue` = `order-placed`
- `RabbitMQ:PaymentProcessedQueue` = `catalog-payment-processed`

## Observações

- As migrações do banco são aplicadas automaticamente ao iniciar a API.
- Para operações de cadastro/edição/remoção de jogos, o token deve conter perfil `Admin`.
