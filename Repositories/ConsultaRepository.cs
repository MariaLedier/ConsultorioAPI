using Dapper;
using Npgsql;
using ConsultorioAPI.Models;

namespace ConsultorioAPI.Repositories
{
    /// <summary>
    /// Repositório de Consultas.
    /// Demonstra o núcleo da estratégia SQL First:
    ///   • Chama a PROCEDURE  agendar_consulta      → insere e dispara a Trigger
    ///   • Chama a FUNCTION   qtd_consultas_por_medico → conta consultas ativas
    ///   • A TRIGGER          trg_impedir_duplicidade → é transparente para a API
    /// </summary>
    public class ConsultaRepository
    {
        private readonly string _connectionString;

        public ConsultaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada.");
        }

        // ── RF03: Agendar — chama a PROCEDURE ────────────────────────────────
        // A Trigger trg_impedir_duplicidade dispara dentro da Procedure.
        // Se houver conflito, o PostgreSQL lança uma exceção que o Controller captura.

        public async Task AgendarConsultaAsync(int idMedico, int idPaciente, DateTime dataHora)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // CALL é a sintaxe correta do PostgreSQL para executar uma Procedure
            var sql = "CALL agendar_consulta(@IdMedico, @IdPaciente, @DataHora)";

            // DateTime.SpecifyKind(Unspecified) garante que o Npgsql envie como
            // 'timestamp' simples, sem fuso horario — igual ao tipo da procedure no banco.
            // Sem isso, o Npgsql envia 'timestamptz' e o PostgreSQL nao encontra a procedure.
            var dataHoraSemFuso = DateTime.SpecifyKind(dataHora, DateTimeKind.Unspecified);

            await connection.ExecuteAsync(sql, new
            {
                IdMedico = idMedico,
                IdPaciente = idPaciente,
                DataHora = dataHoraSemFuso
            });
        }

        // ── RF04: Cancelar ────────────────────────────────────────────────────
        // Atualiza o Status para 'Cancelada' — nunca deleta do banco (boas práticas)

        public async Task<bool> CancelarConsultaAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var linhasAfetadas = await connection.ExecuteAsync(@"
                UPDATE Consultas
                   SET Status = 'Cancelada'
                 WHERE IdConsulta = @Id
                   AND Status    != 'Cancelada'",   // Evita cancelar o que já está cancelado
                new { Id = id });

            return linhasAfetadas > 0;
        }

        // ── RF05: Listagem diária ─────────────────────────────────────────────
        // Retorna consultas de uma data, com filtro opcional por médico.
        // O JOIN traz nome do médico e do paciente para facilitar a exibição.

        public async Task<IEnumerable<Consulta>> GetConsultasDiariaAsync(
            DateTime data, int? idMedico = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // DATE_TRUNC('day', DataHora) compara apenas a parte da data (ignora hora)
            var sql = @"
                SELECT
                    c.IdConsulta,
                    c.IdMedico,
                    c.IdPaciente,
                    c.DataHora,
                    c.Status,
                    m.Nome AS NomeMedico,
                    p.Nome AS NomePaciente
                FROM Consultas c
                INNER JOIN Medicos  m ON m.IdMedico  = c.IdMedico
                INNER JOIN Pacientes p ON p.IdPaciente = c.IdPaciente
                WHERE DATE_TRUNC('day', c.DataHora) = DATE_TRUNC('day', @Data::timestamp)
                  AND (@IdMedico IS NULL OR c.IdMedico = @IdMedico)
                ORDER BY c.DataHora";

            return await connection.QueryAsync<Consulta>(sql, new { Data = data, IdMedico = idMedico });
        }

        // ── RF06: Estatísticas — chama a FUNCTION ────────────────────────────
        // A Function qtd_consultas_por_medico é chamada dentro de um SELECT

        public async Task<int> GetQuantidadeConsultasPorMedicoAsync(int idMedico)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Functions do PostgreSQL são chamadas dentro de um SELECT
            return await connection.QuerySingleOrDefaultAsync<int>(
                "SELECT qtd_consultas_por_medico(@IdMedico)",
                new { IdMedico = idMedico });
        }
    }
}