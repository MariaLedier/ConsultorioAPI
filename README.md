# 🏥 Sistema de Agendamento de Consultas
### ASP.NET Core + PostgreSQL — Estratégia SQL First

---

## 📋 Visão Geral

API REST para controle de agenda médica, com toda a lógica de negócio no banco de dados (SQL First).

| Camada | Tecnologia | Responsabilidade |
|--------|-----------|-----------------|
| API | ASP.NET Core 8 + C# | Receber requisições, repassar ao banco, retornar respostas HTTP |
| ORM | Dapper | Executar SQL puro e mapear resultados para objetos C# |
| Driver | Npgsql | Comunicação C# ↔ PostgreSQL |
| Banco | PostgreSQL | Armazenar dados, validar regras de negócio |

---

## 🗂️ Estrutura de Pastas

```
ConsultorioAPI/
├── SQL/
│   └── 01_criar_banco.sql       ← Execute PRIMEIRO no pgAdmin 4
├── Models/
│   ├── Medico.cs
│   ├── Paciente.cs
│   └── Consulta.cs
├── DTOs/
│   └── Requests.cs
├── Repositories/
│   ├── MedicoRepository.cs
│   ├── PacienteRepository.cs
│   └── ConsultaRepository.cs    ← Chama Procedure e Function
├── Controllers/
│   ├── MedicosController.cs
│   ├── PacientesController.cs
│   └── ConsultasController.cs   ← Captura exceção da Trigger (P0001)
├── Program.cs                   ← Injeção de Dependência + Swagger
├── appsettings.json             ← String de conexão
└── ConsultorioAPI.csproj        ← Pacotes: Npgsql, Dapper, Swagger
```

---

## 🚀 Passo a Passo para Rodar

### Passo 1 — PostgreSQL

1. Instale o PostgreSQL: https://www.postgresql.org/download/
2. Abra o **pgAdmin 4**
3. Crie um banco chamado `ConsultorioDB`
4. Abra a **Query Tool** do novo banco
5. Cole e execute o conteúdo de `SQL/01_criar_banco.sql`

Isso criará:
- Tabelas: `Medicos`, `Pacientes`, `Consultas`
- Índice de performance: `idx_consulta_data_medico`
- Trigger: `trg_impedir_duplicidade` (bloqueia horário duplicado)
- Procedure: `agendar_consulta`
- Function: `qtd_consultas_por_medico`
- Dados de teste (3 médicos + 3 pacientes)

### Passo 2 — Configurar a Senha

Edite `appsettings.json` e substitua `sua_senha_aqui`:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=ConsultorioDB;Username=postgres;Password=SUA_SENHA"
```

### Passo 3 — Rodar no Visual Studio

1. Abra a pasta no Visual Studio (Arquivo → Abrir Pasta)
2. Pressione **F5** ou clique em **Run**
3. O Swagger abrirá em `http://localhost:5000`

### Passo 4 — Instalar pacotes (se necessário)

```bash
dotnet add package Npgsql
dotnet add package Dapper
dotnet add package Swashbuckle.AspNetCore
```

---

## 🔗 Endpoints da API

### Médicos

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/medicos` | Lista todos os médicos |
| `GET` | `/api/medicos/{id}` | Busca médico por ID |
| `POST` | `/api/medicos` | Cadastra novo médico |
| `PUT` | `/api/medicos/{id}` | Edita médico |
| `DELETE` | `/api/medicos/{id}` | Remove médico |

### Pacientes

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/pacientes` | Lista todos os pacientes |
| `GET` | `/api/pacientes/{id}` | Busca paciente por ID |
| `POST` | `/api/pacientes` | Cadastra novo paciente |
| `PUT` | `/api/pacientes/{id}` | Edita paciente |
| `DELETE` | `/api/pacientes/{id}` | Remove paciente |

### Consultas

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/consultas` | Agenda consulta (chama **Procedure**) |
| `PUT` | `/api/consultas/{id}/cancelar` | Cancela consulta |
| `GET` | `/api/consultas/diaria?data=YYYY-MM-DD&idMedico=1` | Lista consultas do dia |
| `GET` | `/api/consultas/medico/{id}/estatisticas` | Qtd consultas (chama **Function**) |

---

## 🧪 Roteiro de Testes no Swagger (Dia 4)

### Teste 1 — Agendar com sucesso
```json
POST /api/consultas
{
  "idMedico": 1,
  "idPaciente": 1,
  "dataHora": "2024-06-10T10:00:00"
}
```
Esperado: **201 Created**

### Teste 2 — Conflito de horário (Trigger em ação!)
```json
POST /api/consultas
{
  "idMedico": 1,
  "idPaciente": 2,
  "dataHora": "2024-06-10T10:00:00"
}
```
Esperado: **409 Conflict**
```json
{ "message": "Conflito de horário: O médico já possui consulta neste horário." }
```

### Teste 3 — Mesmo horário, médico diferente (deve passar)
```json
POST /api/consultas
{
  "idMedico": 2,
  "idPaciente": 1,
  "dataHora": "2024-06-10T10:00:00"
}
```
Esperado: **201 Created**

### Teste 4 — Cancelar consulta
```
PUT /api/consultas/1/cancelar
```
Esperado: **200 OK**

### Teste 5 — Listagem diária
```
GET /api/consultas/diaria?data=2024-06-10
GET /api/consultas/diaria?data=2024-06-10&idMedico=1
```

### Teste 6 — Estatísticas (Function)
```
GET /api/consultas/medico/1/estatisticas
```

---

## 💡 Como a Estratégia SQL First Funciona Aqui

```
Usuário faz POST /api/consultas
        ↓
ConsultasController.Agendar()
        ↓ try
ConsultaRepository.AgendarConsultaAsync()
        ↓
CALL agendar_consulta(...)   ← Procedure no PostgreSQL
        ↓
INSERT INTO Consultas ...
        ↓ (automático, invisível para a API)
TRIGGER trg_impedir_duplicidade
        ↓ conflito de horário?
    SIM → RAISE EXCEPTION 'Conflito de horário...'
        ↓
PostgresException (SqlState = P0001)
        ↓ catch no Controller
return Conflict(409) com mensagem amigável
```

A API **não verifica** se o horário está livre. Ela delega essa responsabilidade ao banco.
Se o banco disser "não", a API repassa o erro — simples, seguro e rápido.

---

## 📦 Pacotes NuGet

| Pacote | Versão | Função |
|--------|--------|--------|
| `Npgsql` | 8.x | Driver oficial PostgreSQL para .NET |
| `Dapper` | 2.x | Micro-ORM: SQL puro → objetos C# |
| `Swashbuckle.AspNetCore` | 6.x | Documentação e UI Swagger |
