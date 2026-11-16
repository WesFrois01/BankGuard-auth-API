# BankGuard Auth & Transactions API

API RESTful de autenticação, transferência financeira atômica e trilha de auditoria construída em **.NET 10 (ASP.NET Core)**. Desenvolvida com foco em arquitetura em camadas, segurança, resiliência e prontidão para deploy em containers.

---

## Principais Recursos e Arquitetura

* **Arquitetura em Camadas:** Separação clara entre *Controllers* (roteamento HTTP), *Services* (regras de negócio isoladas por interfaces `IAuthService` e `ITransactionService`), *DTOs* e camada de persistência (*Data/Models*).
* **Autenticação e Autorização Stateless (JWT):** Emissão e validação de tokens JWT Bearer com `ClockSkew = TimeSpan.Zero` e hash seguro de senhas com **BCrypt**.
* **Integridade Financeira (ACID):** Operações de transferência executadas dentro de transações explícitas do Entity Framework Core (`BeginTransactionAsync`), garantindo atomicidade entre débito, crédito e registro de auditoria (`AuditLogs`).
* **Observabilidade e Auditoria:** Endpoint paginado e filtrável (`GET /api/transactions/audit`) registrando tentativas bem-sucedidas (`SUCCESS`) e bloqueadas (`BLOCKED`).
* **Resiliência e Segurança:**
  * Tratamento global de exceções implementado com `IExceptionHandler` retornando respostas padronizadas **RFC 7807 (`ProblemDetails`)**.
  * Proteção contra ataques de força bruta no endpoint de login via **Rate Limiting** nativo (`FixedWindowLimiter` — 5 requisições/minuto, HTTP `429`).
* **Testes Automatizados:** Suite de testes unitários com **xUnit** e **EF Core In-Memory** validando regras críticas de negócio.
* **Containerização:** Build multi-stage com Docker (`sdk:10.0` + `aspnet:10.0` rodando como usuário não-root) e orquestração via `docker-compose` com persistência de dados em volume.

---

## Stack Tecnológica

* **.NET 10** / ASP.NET Core Web API
* **Entity Framework Core 10** + **SQLite**
* **JWT Bearer Authentication** & **BCrypt.Net**
* **Scalar** (Documentação interativa OpenAPI)
* **xUnit** & **EF Core In-Memory**
* **Docker** & **Docker Compose**

---

## Como Executar

### Opção 1: Via Docker Compose (Recomendado)
```bash
docker compose up --build -d