# Documentação: Fluxo de Compra no CatalogAPI

Este documento explica tudo o que foi adicionado ao CatalogAPI (solução `Jogos.WebAPI`) para suportar o **fluxo de compra orientado a eventos** com RabbitMQ.

---

## 1. Visão geral

O CatalogAPI já fazia o CRUD de jogos. Foi adicionada a responsabilidade de **iniciar a compra** de um jogo e **concluí-la** quando o pagamento for processado.

```
Usuário ──POST /Jogo/{id}/comprar──▶ CatalogAPI
									   │ 1. cria Pedido (Pendente)
									   │ 2. publica OrderPlacedEvent ──▶ fila "order-placed"
									   ▼
								   PaymentsAPI (outro microsserviço)
									   │ consome, simula o pagamento
									   │ publica PaymentProcessedEvent ──▶ fila "payment-processed"
									   ▼
								   CatalogAPI (PaymentProcessedConsumer)
									   │ 3. Approved → Pedido Aprovado + jogo entra na biblioteca
									   │    Rejected → Pedido Rejeitado
```

O CatalogAPI **nunca chama** o PaymentsAPI diretamente. Toda comunicação passa por mensagens no RabbitMQ (desacoplamento).

---

## 2. Arquitetura em camadas

A solução tem 3 projetos:

| Projeto  | Papel | Depende de |
|----------|-------|-----------|
| **Core** | Domínio: entidades, eventos, contratos (interfaces), DTOs, validações. Não conhece banco nem RabbitMQ. | nada |
| **Infra** | Implementações concretas: Entity Framework (SQL Server), repositórios, RabbitMQ. | Core |
| **WebAPI** | Controllers, serviços de aplicação, consumer em background, `Program.cs`. | Infra (e, por ela, Core) |

Regra: as dependências apontam para dentro. O Core define **interfaces** (`IEventPublisher`, `IBibliotecaRepository`) e a Infra as **implementa**.

---

## 3. Core

### 3.1 Alteração em `Jogo` (e DTOs relacionados)
Foi adicionada a propriedade `Preco` (decimal), porque o evento `OrderPlacedEvent` precisa informar o preço.

Arquivos afetados:
- `Entidade/Jogo.cs`: propriedade `Preco`; `Criar` e `Atualizar` passam a copiar o preço.
- `Input/JogoAdicionarRequisicao.cs` e `Input/JogoAtualizarRequisicao.cs`: recebem `Preco`.
- `Output/JogoResposta.cs`: devolve `Preco`.
- `Validation/*Validator.cs`: regra `Preco >= 0`.

### 3.2 Eventos (`Core/Events`)
Eventos são mensagens imutáveis (`record`) trafegadas pelo RabbitMQ em JSON.

- **`OrderPlacedEvent(OrderId, UserId, GameId, Price)`**: publicado pelo Catalog quando a compra é iniciada. Contém o `OrderId` para que, na volta, o pagamento possa ser associado ao pedido correto.
- **`PaymentProcessedEvent(OrderId, UserId, GameId, Status)`**: publicado pelo PaymentsAPI com o resultado.
- **`PaymentStatus`** (enum): `Approved = 1`, `Rejected = 2`.

> Atenção: o enum é serializado como **número** por padrão no `System.Text.Json`. O PaymentsAPI deve enviar `1` ou `2` (ou ambos os lados devem configurar conversão para string).

### 3.3 Mensageria (`Core/Messaging/IEventPublisher.cs`)
- **`Queues`**: constantes com os nomes das filas (`order-placed`, `payment-processed`). Evita "strings mágicas" espalhadas.
- **`IEventPublisher`**: contrato `Publish<T>(string queue, T evento)`. O serviço de compra depende só dessa interface, sem saber que é RabbitMQ (facilita testes e troca de broker).

### 3.4 Entidades novas
Ambas herdam de `BaseEntity` (que fornece `Id`, `Data`, `Ativo`).

- **`Pedido`**: representa uma tentativa de compra.
  - Campos: `UserId`, `JogoId`, `Preco`, `Status`.
  - `PedidoStatus`: `Pendente`, `Aprovado`, `Rejeitado`.
  - `Pedido.Criar(userId, jogo)`: factory que copia o preço do jogo **no momento da compra** (histórico preservado caso o preço mude depois).
- **`BibliotecaJogo`**: liga um usuário a um jogo que ele possui (`UserId`, `JogoId`). Só é criada quando o pagamento é aprovado.

### 3.5 `Repository/IBibliotecaRepository.cs`
Estende `IRepository<BibliotecaJogo>` com:
- `GetByUsuario(userId)`: lista a biblioteca do usuário.
- `Existe(userId, jogoId)`: evita adicionar o mesmo jogo duas vezes.

---

## 4. Infra

### 4.1 `ApplicationDbContext`
Ganhou `DbSet<Pedido>` e `DbSet<BibliotecaJogo>`. As configurações são carregadas automaticamente por `ApplyConfigurationsFromAssembly`.

### 4.2 `Configurations/PedidoConfiguration.cs`
Contém duas configurações do EF:
- `PedidoConfiguration`: tabela `Pedido`, chave `Id` identity, `Preco` como `decimal(18,2)`.
- `BibliotecaJogoConfiguration`: tabela `BibliotecaJogo`, chave `Id` identity e índice em `(UserId, JogoId)` para acelerar a consulta de duplicidade.

`JogoConfiguration` também recebeu `Preco` como `decimal(18,2)`.

### 4.3 `Repository/BibliotecaRepository.cs`
Herda de `EFRepository<BibliotecaJogo>` (que já tem `Add`, `Update`, `GetAll`, `GetById`) e implementa `GetByUsuario` e `Existe`.

Para `Pedido` **não** foi criada classe própria: usa-se `EFRepository<Pedido>` diretamente, registrado como `IRepository<Pedido>`.

### 4.4 `Messaging/RabbitMqPublisher.cs`
Contém duas classes:
- **`RabbitMqSettings`**: host, porta, usuário e senha (lidos da seção `RabbitMq` do `appsettings.json`). O método `CreateFactory()` monta a `ConnectionFactory` (com `DispatchConsumersAsync = true`, necessário para consumers assíncronos).
- **`RabbitMqPublisher`** (implementa `IEventPublisher`): serializa o evento para JSON e publica na fila informada.
  - Declara a fila como durável e a mensagem como persistente (sobrevivem a reinício do broker).
  - Mantém uma conexão/canal reutilizados e recria se caírem; protegido por `lock` porque é singleton e o canal do RabbitMQ não é thread-safe.

Pacote adicionado em `Infra.csproj`: `RabbitMQ.Client 6.8.1`.

---

## 5. WebAPI

### 5.1 `Interface/ICompraService.cs` e `Service/CompraService.cs`
Serviço de aplicação com a lógica do fluxo. Dependências: `IJogoRepository`, `IRepository<Pedido>`, `IBibliotecaRepository`, `IEventPublisher`, `ILogger`.

**`IniciarCompra(userId, jogoId)`**
1. Busca o jogo; se não existir, lança `ExceptionJogoNaoEncontrado` (o `ExceptionMiddleware` converte em 404).
2. Cria o `Pedido` (Pendente) e salva, obtendo o `Id`.
3. Publica `OrderPlacedEvent` com o id do pedido, usuário, jogo e preço.
4. Retorna mensagem de sucesso. A compra **ainda não está concluída**; apenas iniciada.

**`ProcessarPagamento(PaymentProcessedEvent)`**
1. Busca o pedido pelo `OrderId`; se não existir, loga e ignora.
2. Se o pedido não estiver `Pendente`, retorna (**idempotência**: mensagem repetida não causa efeito duplicado).
3. Se `Approved`: marca `Aprovado` e adiciona à `BibliotecaJogo` (se ainda não existir).
4. Se `Rejected`: marca `Rejeitado`.

### 5.2 `Service/PaymentProcessedConsumer.cs`
Um `BackgroundService` (roda junto com a API) que consome a fila `payment-processed`.

- Loop externo: tenta conectar; se o RabbitMQ estiver fora, espera 5s e tenta de novo (a API não quebra ao subir sem o broker).
- Declara a fila e usa `BasicQos(0, 1)` (processa uma mensagem por vez).
- Ao receber mensagem: desserializa o JSON, **cria um escopo de DI** e resolve `ICompraService`, e chama `ProcessarPagamento`.
  - O escopo é necessário porque o consumer é singleton e os repositórios/DbContext são *scoped*.
- **Ack manual**: sucesso → `BasicAck`; erro → `BasicNack` sem reenfileirar (evita loop infinito de mensagem venenosa; em produção o ideal seria uma dead-letter queue).

### 5.3 `Controllers/JogoController.cs`: novo endpoint
`POST /Jogo/{id}/comprar` com `[Authorize]`:
- Lê o id do usuário do JWT (claim `NameIdentifier` ou `sub`). Se não for um inteiro válido, retorna 401.
- Chama `IniciarCompra` e retorna **202 Accepted** (aceito, processamento assíncrono).

### 5.4 `Program.cs`
Registros de DI adicionados:
- `ICompraService → CompraService` (scoped)
- `IRepository<Pedido> → EFRepository<Pedido>` (scoped)
- `IBibliotecaRepository → BibliotecaRepository` (scoped)
- `RabbitMqSettings` (singleton, lido da configuração)
- `IEventPublisher → RabbitMqPublisher` (singleton, reaproveita a conexão)
- `PaymentProcessedConsumer` como `HostedService`

### 5.5 `appsettings.json`
Nova seção:
```json
"RabbitMq": { "Host": "localhost", "Port": 5672, "User": "guest", "Password": "guest" }
```

---

## 6. Como as classes se relacionam

```
JogoController
   └─ ICompraService ◀── CompraService
							├─ IJogoRepository ◀── JogoRepository (Infra)
							├─ IRepository<Pedido> ◀── EFRepository<Pedido> (Infra)
							├─ IBibliotecaRepository ◀── BibliotecaRepository (Infra)
							└─ IEventPublisher ◀── RabbitMqPublisher (Infra)

PaymentProcessedConsumer (BackgroundService)
   ├─ RabbitMqSettings
   └─ (escopo) ICompraService.ProcessarPagamento(...)
```

Sequência de uma compra aprovada:
1. `JogoController.Comprar` → `CompraService.IniciarCompra`
2. `EFRepository<Pedido>.Add` grava o pedido; `RabbitMqPublisher.Publish` envia `OrderPlacedEvent`.
3. (PaymentsAPI processa e publica `PaymentProcessedEvent`.)
4. `PaymentProcessedConsumer` recebe → `CompraService.ProcessarPagamento` → atualiza `Pedido` e grava `BibliotecaJogo`.

---

## 7. O que ainda precisa ser feito

1. **Migration do EF**: criar e aplicar (tabelas `Pedido`, `BibliotecaJogo` e coluna `Jogo.Preco`).
   ```
   dotnet ef migrations add FluxoCompra -p Infra -s WebAPI
   dotnet ef database update -p Infra -s WebAPI
   ```
2. **RabbitMQ rodando** (ex.: `docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management`).
3. **Contrato com os outros serviços**:
   - PaymentsAPI consome `order-placed` e publica `payment-processed` com o JSON no mesmo formato (propriedades PascalCase: `OrderId`, `UserId`, `GameId`, `Status`).
   - UsersAPI deve gerar o JWT com o id numérico do usuário em `sub`/`NameIdentifier`, usando o mesmo `Jwt:Key` e `Jwt:Issuer`.
4. **Opcional**: endpoint para listar a biblioteca do usuário (`GetByUsuario` já existe), dead-letter queue e testes.

---

## 8. Como testar manualmente
1. Suba o RabbitMQ e rode a API.
2. Autentique-se (token JWT com `sub` numérico) e chame `POST /Jogo/1/comprar`.
3. No painel do RabbitMQ (`http://localhost:15672`, guest/guest) veja a mensagem na fila `order-placed`.
4. Simule o PaymentsAPI publicando manualmente na fila `payment-processed`:
   ```json
   { "OrderId": 1, "UserId": 5, "GameId": 1, "Status": 1 }
   ```
5. Confira no banco: `Pedido.Status = 2` (Aprovado) e um registro em `BibliotecaJogo`.
