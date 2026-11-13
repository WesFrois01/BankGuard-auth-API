# 🛡️ BankGuard Auth API
 
API RESTful de autenticação bancária e movimentação financeira segura desenvolvida em **.NET 10**, projetada com foco em boas práticas de **Segurança da Informação (AppSec)**, rastreabilidade e arquitetura limpa.
 
---
 
## 🚀 Arquitetura e Recursos de Segurança
 
- **Hashing Criptográfico de Senhas (`BCrypt.Net-Next`)**: Armazenamento seguro de credenciais com *salt* automático, prevenindo ataques de *rainbow table* e força bruta.
- **Autenticação Stateless via JWT (`HMAC-SHA256`)**: Emissão de tokens assinados digitalmente com expiração curta (30 minutos) e validação rigorosa de *Issuer*, *Audience* e *Lifetime*.
- **Prevenção contra Enumeração de Contas**: Respostas padronizadas (`401 Unauthorized`) em falhas de login para não expor e-mails cadastrados na base.
- **Prevenção contra IDOR / Fraude de Identidade**: A identificação do remetente em transferências é extraída exclusivamente da *claim* `sub` do Token JWT autenticado, ignorando identificadores manipuláveis no corpo da requisição.
- **Trilha de Auditoria Imutável (`AuditLogs`)**: Registo detalhado com identificador único (GUID) e carimbo de tempo UTC para todas as operações aprovadas (`SUCCESS`) e tentativas bloqueadas (`BLOCKED` por saldo insuficiente ou destinatário inválido).
- **Documentação Interativa**: Interface OpenAPI integrada com **Scalar**.
 
---
 
## 🛠️ Tecnologias Utilizadas
 
- **C# / .NET 10 (ASP.NET Core Web API)**
- **BCrypt.Net-Next**
- **System.IdentityModel.Tokens.Jwt**
- **Scalar.AspNetCore** (OpenAPI 3.1)
 
---
 
## 📌 Endpoints Principais
 
| Método | Rota | Autenticação | Descrição |
| :--- | :--- | :---: | :--- |
| `POST` | `/api/Auth/register` | Pública | Regista nova conta com hash BCrypt e saldo inicial |
| `POST` | `/api/Auth/login` | Pública | Valida credenciais e emite Token JWT assinado |
| `POST` | `/api/Transactions/transfer` | **JWT Bearer** | Executa transferência entre contas e gera log de auditoria |
| `GET` | `/api/Transactions/audit` | **JWT Bearer** | Consulta o histórico imutável de auditoria financeira |
 
---
 
## ▶️ Como Executar Localmente
 
```bash
# Clonar o repositório
git clone [https://github.com/WesFrois01/BankGuard-auth-API.git](https://github.com/WesFrois01/BankGuard-auth-API.git)
cd BankGuard-auth-API
 
# Restaurar dependências e iniciar a API
dotnet restore
dotnet run
 