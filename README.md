# Fluxo de Caixa – Desafio Backend (.NET)

Implementação de uma solução backend para controle de **fluxo de caixa diário**, com registro de **lançamentos financeiros** e consulta de **saldo diário consolidado**, conforme o desafio técnico proposto.

O foco do projeto é demonstrar **qualidade de código**, **arquitetura**, **resiliência**, **escalabilidade** e **boas práticas** de engenharia de software.

<img width="1536" height="1024" alt="d5e4bb3c-001a-41ef-8da9-b83a318f83e9" src="https://github.com/user-attachments/assets/426f7d9d-cdba-436b-ad5c-0c2e912b7ee9" />

---

## 🎯 Objetivo do Desafio

- Serviço transacional para controle de lançamentos (crédito/débito)
- Serviço de relatório para saldo diário consolidado
- Arquitetura resiliente, escalável e desacoplada
- Código limpo, testável e aderente a SOLID, DDD e Clean Code

---

## 🧱 Arquitetura da Solução

A solução foi implementada utilizando **arquitetura baseada em eventos**, com serviços desacoplados e comunicação assíncrona.

### Componentes:

- **Lancamentos.API**
  - Serviço transacional
  - Persistência com EF Core
  - Publicação de eventos após commit

- **Consolidado.Worker**
  - Consumo assíncrono de eventos
  - Consolidação do saldo diário
  - Implementação de **idempotência**

- **Consolidado.API**
  - Serviço de leitura (read-only)
  - Consulta de saldo consolidado
  - Leitura otimizada com **Dapper**

- **RabbitMQ**
  - Mensageria para desacoplamento e resiliência

- **PostgreSQL**
  - Bancos independentes por contexto

---

## 🧠 Decisões Técnicas Relevantes

- **Arquitetura Orientada a Eventos**
  - Garante que o serviço de lançamentos não dependa da disponibilidade do consolidado
- **Separação Write / Read (CQRS)**
  - Escrita com EF Core
  - Leitura com Dapper
- **DDD**
  - Separação clara entre domínio, aplicação e infraestrutura
- **Idempotência no Worker**
  - Evita duplicidade em reprocessamentos
- **Stateless services**
  - Permite escalabilidade horizontal

---

## ⚙️ Stack Tecnológica

- C#
- .NET 8
- ASP.NET Core
- Entity Framework Core
- Dapper
- RabbitMQ
- PostgreSQL
- Docker / Docker Compose

---

## 📈 Requisitos Não Funcionais Atendidos

- **Escalabilidade**
  - Serviços stateless
  - Processamento assíncrono
- **Resiliência**
  - Desacoplamento via mensageria
  - Tolerância à indisponibilidade do serviço de consolidado
- **Disponibilidade**
  - Serviço transacional independente do relatório
- **Desempenho**
  - Read model otimizado para até **50 RPS**

---

## ▶️ Execução Local

### Pré-requisitos
- Docker
- Docker Compose
- .NET SDK 8+

## 1. Clone o repositório:
git clone https://github.com/Marcelobsdo/FluxoCaixa.git

## 2. Suba a infraestrutura (PostgreSQL e RabbitMQ):

cd FluxoCaixa

docker compose up -d postgres-lancamentos postgres-consolidado rabbitmq

## 3. Aplique as migrations nos bancos de dados:

dotnet ef database update --project src/Lancamentos.Infrastructure --startup-project src/Lancamentos.API --context LancamentosDbContext

dotnet ef database update --project src/Consolidado.Infrastructure --context ConsolidadoDbContext

## 4. Suba os serviços da aplicação:

docker compose up -d --build

Após a inicialização:

Lancamentos.API: criação de lançamentos --> http://localhost:5000/swagger/index.html

Consolidado.API: consulta do saldo diário consolidado --> http://localhost:6002/swagger/index.html

🔄 Fluxo End-to-End

Lançamento criado via Lancamentos.API

Evento publicado no RabbitMQ

Consolidado.Worker consome o evento

Saldo diário consolidado é persistido

Consulta via Consolidado.API

🧪 Testabilidade

A solução foi estruturada para facilitar testes:

Domínio desacoplado de infraestrutura

Dependências invertidas via IoC

Serviços com responsabilidades bem definidas

🔮 Possíveis Evoluções

Autenticação e autorização (OAuth2 / OIDC)

Observabilidade (logs estruturados, métricas)

Retry / Circuit Breaker

Cache no read service

Pipeline CI/CD

✅ Considerações Finais
O projeto prioriza decisões arquiteturais corretas, qualidade de código e aderência aos requisitos não funcionais, demonstrando capacidade de projetar e implementar soluções backend escaláveis, resilientes e bem estruturadas.


