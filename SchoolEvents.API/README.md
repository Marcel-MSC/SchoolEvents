# ⚙️ School Events API

Backend do sistema de gerenciamento de eventos escolares desenvolvido em ASP.NET Core.

## 🛠️ Tecnologias

- **ASP.NET Core 6** - Framework web
- **Entity Framework Core** - ORM
- **SQL Server** - Banco de dados
- **Azure AD** - Autenticação
- **Swagger** - Documentação API

## 🚀 Execução

```bash
# Restaurar pacotes
dotnet restore

# Executar aplicação
dotnet clean && dotnet build && dotnet run

# Executar migrações
dotnet ef database update