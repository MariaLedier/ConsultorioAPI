using Dapper;
using Npgsql;
using ConsultorioAPI.Models;
using ConsultorioAPI.DTOs;

namespace ConsultorioAPI.Repositories
{
    /// <summary>
    /// Responsável por toda a comunicação com o banco relacionada a Médicos.
    /// Usa Dapper para mapear resultados SQL → objetos C# de forma direta e rápida.
    /// </summary>
    public class MedicoRepository
    {
        private readonly string _connectionString;

        // O IConfiguration lê o appsettings.json e injeta a string de conexão
        public MedicoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada.");
        }

        // ── RF01: Listar todos ────────────────────────────────────────────────

        public async Task<IEnumerable<Medico>> GetAllAsync()
        {
            // 'using var' garante que a conexão é FECHADA automaticamente ao sair do escopo
            using var connection = new NpgsqlConnection(_connectionString);

            // Dapper executa o SQL e mapeia cada linha para um objeto Medico
            return await connection.QueryAsync<Medico>("SELECT * FROM Medicos ORDER BY Nome");
        }

        public async Task<Medico?> GetByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // QueryFirstOrDefaultAsync retorna o primeiro resultado ou null
            return await connection.QueryFirstOrDefaultAsync<Medico>(
                "SELECT * FROM Medicos WHERE IdMedico = @Id",
                new { Id = id });
        }

        // ── RF01: Cadastrar ───────────────────────────────────────────────────

        public async Task<int> AddAsync(MedicoRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // RETURNING IdMedico faz o PostgreSQL devolver o ID gerado automaticamente
            var sql = @"
                INSERT INTO Medicos (Nome, CRM, Especialidade)
                VALUES (@Nome, @CRM, @Especialidade)
                RETURNING IdMedico";

            // Dapper substitui @Nome, @CRM, @Especialidade pelos valores do objeto request
            return await connection.QuerySingleAsync<int>(sql, request);
        }

        // ── RF01: Editar ──────────────────────────────────────────────────────

        public async Task<bool> UpdateAsync(int id, MedicoRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                UPDATE Medicos
                   SET Nome          = @Nome,
                       CRM           = @CRM,
                       Especialidade = @Especialidade
                 WHERE IdMedico = @Id";

            // ExecuteAsync retorna o número de linhas afetadas
            var linhasAfetadas = await connection.ExecuteAsync(sql, new
            {
                request.Nome,
                request.CRM,
                request.Especialidade,
                Id = id
            });

            return linhasAfetadas > 0; // true = médico encontrado e atualizado
        }

        // ── RF01: Excluir ─────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var linhasAfetadas = await connection.ExecuteAsync(
                "DELETE FROM Medicos WHERE IdMedico = @Id",
                new { Id = id });

            return linhasAfetadas > 0;
        }
    }
}
