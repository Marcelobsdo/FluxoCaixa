# ADR-001 — Decisões Arquiteturais e Roadmap de Evolução

## Contexto

Este projeto foi desenvolvido como um desafio técnico de backend para controle de fluxo de caixa diário, envolvendo:

- Registro de lançamentos financeiros (crédito/débito)
- Consolidação de saldo diário
- Arquitetura resiliente, desacoplada e escalável
- Execução local simples via Docker Compose

## Decisões Arquiteturais

### 1. Arquitetura Orientada a Eventos

**Decisão**  
Utilizar mensageria (RabbitMQ) para desacoplar o serviço transacional do serviço de consolidação.

**Justificativa**
- Evita dependência síncrona entre serviços
- Garante que lançamentos possam ser registrados mesmo com o consolidado indisponível
- Facilita escalabilidade e reprocessamento

**Consequências**
- Processamento eventual (eventual consistency)
- Necessidade de idempotência no consumidor

---

### 2. Separação de Contextos e Bancos de Dados

**Decisão**  
Cada contexto possui seu próprio banco PostgreSQL.

- `Lancamentos` → banco transacional
- `Consolidado` → banco de leitura/relatório

**Justificativa**
- Evita acoplamento entre modelos
- Permite otimização independente
- Aderência a DDD e Bounded Context

**Consequências**
- Dados são duplicados de forma controlada
- Consolidação ocorre via eventos

---

### 3. CQRS Simples

**Decisão**
- Escrita com EF Core
- Leitura com Dapper

**Justificativa**
- Escrita transacional segura
- Leitura performática e otimizada
- Simplicidade

**Consequências**
- Dois modelos de acesso a dados
- Código mais explícito e fácil de manter

---

### 4. Worker de Consolidação com Idempotência

**Decisão**
Implementar idempotência no `Consolidado.Worker` usando a tabela `processed_event`.

**Justificativa**
- Evita duplicidade em reprocessamentos
- Permite retry seguro
- Garante consistência do saldo

**Consequências**
- Controle explícito de eventos processados
- Pequeno custo adicional de persistência

---

### 5. Migração Automática no Worker (Development)

**Decisão**
Aplicar migrations automaticamente no startup do Worker **apenas em ambiente Development**.

**Justificativa**
- Facilita execução local do avaliador
- Evita passos manuais adicionais
- Não compromete boas práticas de produção

**Consequências**
- Em produção, migrations devem ser controladas via pipeline
- Comportamento condicionado ao ambiente

---

### 6. Portas Customizadas no Docker Compose

**Decisão**
Utilizar portas explícitas e não-padrão para evitar conflitos locais:

- `Lancamentos.API` → `6001`
- `Consolidado.API` → `6002`
- PostgreSQL Lancamentos → `5432`
- PostgreSQL Consolidado → `5433`

**Justificativa**
- Evita conflitos com serviços locais
- Facilita execução paralela de outros projetos

---

## Fora do Escopo do Desafio (Evoluções Naturais)

Os itens abaixo representam melhorias comuns em sistemas financeiros em produção, **intencionalmente fora do escopo do desafio**, mas consideradas no desenho da arquitetura.

### Retry e Dead Letter Queue (DLQ)

- Retry por mensagem no consumer
- Após exceder tentativas, envio para DLQ (TTL/DLX)
- Análise e reprocessamento manual

### Publicação Garantida de Eventos

- Padrão Outbox
- Garantia de entrega mesmo em falhas no broker

### Observabilidade

- Logs estruturados
- Tracing distribuído (OpenTelemetry)
- Métricas e dashboards

### Cache no Read Model

- Redis para consultas repetidas por comerciante/dia
- Redução de carga no banco de leitura

### Segurança e Configuração de Produção

- Validação de issuer/audience JWT
- Rotação de chaves
- Secrets Vault
- HSTS e headers de segurança

### CI/CD

- Pipeline com:
  - Build
  - Testes
  - Análise estática
  - Build e publish de imagens Docker

---

## Conclusão

A arquitetura foi desenhada para:

- Resolver o problema proposto com simplicidade
- Demonstrar maturidade arquitetural
- Permitir evolução incremental sem refatorações disruptivas

As decisões priorizaram **clareza, desacoplamento e robustez**, mantendo o escopo adequado ao desafio técnico.
