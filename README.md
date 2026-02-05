# Fluxo de Caixa – Desafio Backend (.NET 8)

Solução backend para **controle de fluxo de caixa diário**, com:
- **Serviço transacional** para registrar lançamentos (crédito/débito)
- **Pipeline assíncrono** via RabbitMQ para consolidar lançamentos
- **Serviço de leitura** para consultar o **saldo diário consolidado**

O foco do projeto é demonstrar **arquitetura**, **qualidade de código**, **resiliência**, e uma **experiência de execução previsível** para o avaliador.

---

<img width="1536" height="1024" alt="d5e4bb3c-001a-41ef-8da9-b83a318f83e9" src="https://github.com/user-attachments/assets/426f7d9d-cdba-436b-ad5c-0c2e912b7ee9" />

## Visão rápida (o que você precisa saber)

- Swagger Lancamentos.API: **http://localhost:6001/swagger**
- Swagger Consolidado.API: **http://localhost:6002/swagger**
- RabbitMQ Management: **http://localhost:15672** (user/pass: `guest` / `guest`)
- Postgres Lancamentos (host): `localhost:5432`
- Postgres Consolidado (host): `localhost:5433`

> Observação: `/swagger` retorna **301** para `swagger/index.html`. Com `curl`, use `-L` para seguir redirect.

---

## Arquitetura da solução

A solução foi implementada usando **arquitetura baseada em eventos**, com serviços desacoplados e comunicação assíncrona.

**Componentes**

### 1) Lancamentos.API (write)
- Endpoint **POST /api/lancamentos**
- Persistência com **EF Core (PostgreSQL)**
- Publicação de evento no RabbitMQ após commit
- Endpoints protegidos com **JWT Bearer**

### 2) RabbitMQ (mensageria)
- Exchange: `lancamentos.exchange` (fanout)
- Queue: `lancamentos-criados` (durable)

### 3) Consolidado.Worker (consumer)
- Consome eventos e consolida lançamentos no banco do contexto Consolidado
- **Idempotência**: tabela `processed_event` evita reprocessamento
- **Resiliência no startup**: tolera RabbitMQ ainda inicializando (retry no startup + auto-recovery do client)
- Escrita com **EF Core (PostgreSQL)**
- Logs estruturados com **Serilog**

### 4) Consolidado.API (read)
- Endpoint **GET /api/consolidado/saldo-diario**
- Leitura otimizada com **Dapper**
- Endpoints protegidos com **JWT Bearer**

---

## Decisões técnicas principais

- **Event-driven**: o serviço de lançamentos não depende da disponibilidade do consolidado.
- **Separação Write/Read** (CQRS leve):
  - Write: EF Core
  - Read: Dapper
- **Idempotência no Worker**: proteção contra duplicidade em reentrega/reprocessamento.
- **Serviços stateless**: facilita escala horizontal.
- **JWT nos endpoints**: evita endpoints públicos em um cenário financeiro.
- **Migrations automáticas no ambiente Development**: para reduzir fricção na avaliação.

---

## Stack

- .NET 8 / ASP.NET Core
- C#
- PostgreSQL 16
- RabbitMQ 3 (management)
- EF Core 8 (write)
- Dapper (read)
- Serilog (logs)
- Docker / Docker Compose

---

## Como executar

### Pré-requisitos
- Docker Desktop (Windows/macOS) ou Docker Engine (Linux)
- Docker Compose

> **Não é necessário** instalar .NET SDK para rodar a solução via Docker.

### 1) Limpar ambiente (opcional, recomendado para “from scratch”)

```powershell
# dentro da pasta do repo
# encerra containers e remove volumes (zera os bancos)
docker compose down -v --remove-orphans

# opcional: remove sobras gerais (imagens/containers não usados)
docker system prune -f
```

### 2) Subir tudo

```powershell
docker compose up -d --build
```

Verifique o status:

```powershell
docker compose ps
```

> Se você receber erro de pipe do Docker no Windows, normalmente é só abrir o **Docker Desktop**.

### 3) Aguardar inicialização

- O Postgres e o RabbitMQ podem levar alguns segundos.
- Em `Development`, o **Lancamentos.API** aplica suas migrations automaticamente ao iniciar.
- Em `Development`, o **Consolidado.Worker** aplica migrations do Consolidado (com retry curto de “DB ainda não pronta”).

### 4) Abrir Swaggers

- Lancamentos: `http://localhost:6001/swagger`
- Consolidado: `http://localhost:6002/swagger`

---

## Autenticação (JWT) – como gerar um token para testar

Os endpoints exigem `Authorization: Bearer <JWT>`.

No `docker-compose.yml`, a chave está definida como:

- `Jwt__Key: "fluxo-caixa-secret-para-o-teste-987654321"`

### Opção A (PowerShell) – gerar um JWT localmente

Execute no PowerShell (Windows):

```powershell
$jwtKey = "fluxo-caixa-secret-para-o-teste-987654321"

Add-Type -AssemblyName System.IdentityModel.Tokens.Jwt
Add-Type -AssemblyName Microsoft.IdentityModel.Tokens

$securityKey = New-Object Microsoft.IdentityModel.Tokens.SymmetricSecurityKey([Text.Encoding]::UTF8.GetBytes($jwtKey))
$creds = New-Object Microsoft.IdentityModel.Tokens.SigningCredentials($securityKey, [Microsoft.IdentityModel.Tokens.SecurityAlgorithms]::HmacSha256)

$now = [DateTime]::UtcNow
$tokenDescriptor = New-Object Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
$tokenDescriptor.Expires = $now.AddHours(1)
$tokenDescriptor.SigningCredentials = $creds

$handler = New-Object System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler
$token = $handler.CreateToken($tokenDescriptor)
$jwt = $handler.WriteToken($token)

$jwt
```

Guarde o resultado em uma variável:

```powershell
$token = "<cole aqui o JWT gerado>"
```

> Observação: por padrão, não validamos `issuer`/`audience` para simplificar o teste local.

---

## Teste end-to-end (E2E)

### 1) Criar um lançamento

```powershell
curl -Method POST "http://localhost:6001/api/lancamentos" `
  -Headers @{ "Content-Type"="application/json"; "Authorization"="Bearer $token" } `
  -Body '{
    "comercianteId":"11111111-1111-1111-1111-111111111111",
    "valor":10.50,
    "tipo":1,
    "data":"2026-02-05T12:00:00Z"
  }'
```

- Retorno esperado: **201 Created**

### 2) Verificar processamento no Worker

```powershell
docker logs -n 200 consolidado-worker
```

Você deve ver algo como:
- `Evento ... consolidado com sucesso`

> Se o RabbitMQ ainda não tiver iniciado, o Worker pode logar tentativas de reconexão no startup por alguns segundos.

### 3) Consultar o saldo diário consolidado

```powershell
curl -Method GET "http://localhost:6002/api/consolidado/saldo-diario?comercianteId=11111111-1111-1111-1111-111111111111&dia=2026-02-05T00:00:00Z" `
  -Headers @{ "Authorization"="Bearer $token" }
```

- Retorno esperado: **200 OK** com JSON:

```json
{
  "comercianteId": "11111111-1111-1111-1111-111111111111",
  "dia": "2026-02-05T00:00:00",
  "totalCreditos": 10.5,
  "totalDebitos": 0,
  "saldo": 10.5
}
```

---

## Observabilidade

- APIs e Worker usam logs estruturados via **Serilog** (console)
- Existe **Correlation-Id** nas respostas das APIs (`X-Correlation-Id`) para facilitar rastreamento em logs

---

## Resiliência e confiabilidade (o que já está coberto)

- **Desacoplamento via RabbitMQ** (Lancamentos não depende do Consolidado)
- **Idempotência** no consumer (`processed_event`)
- **Migrations automáticas** (apenas em `Development`) para reduzir atrito em avaliação
- **RabbitMQ Client com auto-recovery** + retry de startup no Worker (tolerante ao boot do RabbitMQ)

---

## Troubleshooting rápido

### Swagger retorna 404 em `/swagger/index.html` via curl
- Use o endpoint `/swagger` (ele responde `301`)
- Ou siga redirect:

```powershell
curl.exe -L -I http://localhost:6001/swagger
```

### Worker falha com ".NET framework not found"
- Garanta que o Dockerfile do Worker use runtime `mcr.microsoft.com/dotnet/aspnet:8.0` (já está configurado no projeto atual).

### Worker reclama que a tabela `processed_event` não existe
- Isso indica que as migrations do Consolidado não foram aplicadas.
- Solução “do zero”: derrube e suba com volume novo:

```powershell
docker compose down -v --remove-orphans
docker compose up -d --build
```

---

## Possíveis evoluções (ROADMAP)

> Os itens abaixo são melhorias típicas para um serviço financeiro em produção. No desafio, priorizei o essencial para execução local e robustez do pipeline.

- **DLQ/Retry por mensagem no consumer**
  - Implementar retry com contador e, ao exceder, enviar para uma **Dead Letter Queue** (DLX/TTL) para análise.
- **Retry/Exponential backoff no publisher**
  - Garantir publicação robusta (ou padrão **Outbox** para entrega garantida).
- **Teste de carga automatizado**
  - Script k6/JMeter/nbomber para validar requisito de **50 RPS**.
- **Observabilidade completa**
  - Tracing distribuído (OpenTelemetry), métricas (Prometheus) e dashboards.
- **Cache no read model**
  - Ex.: Redis para consultas repetidas por comerciante/dia.
- **Config de produção**
  - Validar issuer/audience, rotação de chave JWT, secrets vault, HSTS, etc.
- **CI/CD**
  - Pipeline com build, testes, lint, análise estática e publish de imagem.

---

## Licença

Uso livre para fins de avaliação do desafio.


