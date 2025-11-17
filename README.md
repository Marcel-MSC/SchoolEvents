# 🏫 School Events - Sistema de Gerenciamento de Eventos Escolares

Sistema completo para gerenciamento de eventos escolares desenvolvido com arquitetura full-stack.

## 🚀 Tecnologias Utilizadas

### Frontend
- **Reactjs** - Components
- **TypeScript** - Tipagem estática
- **Vite** - Build tool
- **Tailwind CSS** - Estilização
- **Axios** - Cliente HTTP

### Backend
- **ASP.NET Core** - Framework web
- **Entity Framework Core** - ORM
- **SQL Server** - Banco de dados
- **Azure AD** - Autenticação

## 📋 Funcionalidades

- ✅ Login com JWT (fluxo de autenticação completo)
- ✅ Listagem paginada de usuários da instituição (dados vindos do Microsoft Graph e armazenados em banco interno)
- ✅ Listagem de eventos por usuário (via banco interno; opção de seed para testes)
- ✅ Filtro "Somente com eventos" no Dashboard (checkbox que envia `onlyWithEvents=true` para a API)
- ✅ Interface responsiva em React + Tailwind
- ✅ API RESTful com documentação via Swagger
- ✅ Jobs de background com Hangfire para sincronização e métricas

## 🔗 Endpoints principais

### Autenticação
- `POST /api/auth/login` – login com email/senha, retorna JWT.
- `POST /api/auth/validate` – valida o token JWT atual.

### Usuários e eventos
- `GET /api/users` – lista paginada de usuários.
- `GET /api/users/{userId}/events` – lista eventos de um usuário (a partir do banco).
- `POST /api/users/sync` – sincroniza usuários (e tenta sincronizar eventos) do Microsoft Graph para o banco.
- `POST /api/users/seed-test-events` – cria eventos de teste para alguns usuários (útil para avaliar o frontend mesmo sem eventos reais no Graph).
- `GET /api/users/debug-sync` – diagnóstico de sincronização (contagens em Graph e banco).

### Infraestrutura
- `GET /api/public/test` – health-check público simples.
- `GET /health` – health-check padrão do ASP.NET Core.
- `GET /swagger` – documentação interativa da API.
- `/hangfire` – painel do Hangfire (requer estar autenticado).

## 🛠️ Como Executar

### Pré-requisitos
- Node.js 18+
- .NET 8.0 SDK
- SQL Server (ex.: `.\\SQLEXPRESS`)

### Backend (API)

1. Navegar até a pasta da API:
   ```bash
   cd SchoolEvents.API
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

2. A API subirá em uma porta dinâmica (por padrão algo como `http://localhost:5101`).
   - Swagger: `http://localhost:5101/swagger`
   - Health: `http://localhost:5101/health`

### Frontend (React)

1. Em outra janela/terminal:
   ```bash
   cd school-events-frontend
   npm install
   npm run dev
   ```

2. A aplicação ficará disponível em `http://localhost:5173`.

### Fluxo sugerido para avaliação

1. Subir backend e frontend como descrito acima.
2. Acessar o frontend em `http://localhost:5173`.
3. Fazer login com as credenciais de teste:
   - Email: `admin@escola.com`
   - Senha: `admin123`
4. Na API (via Swagger ou Postman), com token válido, executar:
   - `POST /api/users/sync` – sincronizar usuários do Microsoft Graph.
   - `POST /api/users/seed-test-events` – criar eventos de teste para alguns usuários.
5. Voltar ao frontend, no Dashboard:
   - Usar a lista de usuários à esquerda para selecionar uma pessoa.
   - Visualizar os eventos no painel da direita.
   - Opcionalmente marcar o checkbox **"Somente com eventos"** para listar apenas usuários que possuem eventos (usa o parâmetro `onlyWithEvents=true` na API).

## 🧪 Como rodar os testes

### Testes de backend (API)

Na raiz da solução:

```bash
dotnet test SchoolEvents.sln
```

Isso executa os testes xUnit do projeto `SchoolEvents.API.Tests`.

*(Atualmente não há testes automatizados no frontend; os testes estão focados na API.)*
