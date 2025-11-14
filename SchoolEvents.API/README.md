# School Events — Teste Técnico

## O que implementa
- Backend: .NET 8 Web API com EF Core (SQL Server), JWT auth, Swagger.
- BackgroundService que sincroniza usuários e eventos do Microsoft Graph (client credentials).
- Frontend: React + Vite + TypeScript + Tailwind.
- Testes unitários com xUnit.
- Migrations do EF Core.

## Requisitos
- .NET 8 SDK
- Node.js 18+
- SQL Server (ou Docker)
- (Opcional) Docker

## Rodando localmente (exemplo)
### Backend
1. Ajuste variáveis de ambiente:
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpireMinutes`
   - `Graph__ClientId`, `Graph__ClientSecret`, `Graph__TenantId`
2. Criar base e rodar migrations:
```bash
cd backend/src/SchoolEvents.Api
dotnet ef database update
dotnet run
