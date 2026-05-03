using Dapper;
using Npgsql;
using ConsultorioAPI.Models;
using ConsultorioAPI.DTOs;

namespace ConsultorioAPI.Repositories
{
    /// <summary>
    /// Responsável por toda a comunicação com o banco relacionada a Pacientes.
    /// </summary>
    public class PacienteRepository
    {
        private readonly string _connectionString;

        public PacienteRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada.");
        }

        // ── RF02: Listar todos ────────────────────────────────────────────────

        public async Task<IEnumerable<Paciente>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Paciente>("SELECT * FROM Pacientes ORDER BY Nome");
        }

        public async Task<Paciente?> GetByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Paciente>(
                "SELECT * FROM Pacientes WHERE IdPaciente = @Id",
                new { Id = id });
        }

        // ── RF02: Cadastrar ───────────────────────────────────────────────────

        public async Task<int> AddAsync(PacienteRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                INSERT INTO Pacientes (Nome, CPF, Telefone)
                VALUES (@Nome, @CPF, @Telefone)
                RETURNING IdPaciente";

            return await connection.QuerySingleAsync<int>(sql, request);
        }

        // ── RF02: Editar ──────────────────────────────────────────────────────

        public async Task<bool> UpdateAsync(int id, PacienteRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                UPDATE Pacientes
                   SET Nome     = @Nome,
                       CPF      = @CPF,
                       Telefone = @Telefone
                 WHERE IdPaciente = @Id";

            var linhasAfetadas = await connection.ExecuteAsync(sql, new
            {
                request.Nome,
                request.CPF,
                request.Telefone,
                Id = id
            });

            return linhasAfetadas > 0;
        }

        // ── RF02: Excluir ─────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var linhasAfetadas = await connection.ExecuteAsync(
                "DELETE FROM Pacientes WHERE IdPaciente = @Id",
                new { Id = id });

            return linhasAfetadas > 0;
        }
    }
}
