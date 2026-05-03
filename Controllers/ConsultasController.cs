using ConsultorioAPI.DTOs;
using ConsultorioAPI.Models;
using ConsultorioAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ConsultorioAPI.Controllers
{
    [ApiController]
    [Route("api/consultas")]
    [Produces("application/json")]
    [Tags("Consultas")]
    public class ConsultasController : ControllerBase
    {
        private readonly ConsultaRepository _repo;
        public ConsultasController(ConsultaRepository repo) => _repo = repo;

        /// <summary>Agendar consulta</summary>
        /// <remarks>
        /// Agenda uma consulta vinculando um médico, um paciente e um horário.
        /// A validação de conflito de horário é feita automaticamente pelo banco de dados (Trigger).
        ///
        /// Exemplo de requisição:
        ///
        ///     POST /api/consultas
        ///     {
        ///         "idMedico": 1,
        ///         "idPaciente": 1,
        ///         "dataHora": "2024-06-10T10:00:00"
        ///     }
        ///
        /// Retorna **409 Conflict** se o médico já tiver consulta neste horário.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Agendar([FromBody] AgendamentoRequest request)
        {
            try
            {
                await _repo.AgendarConsultaAsync(request.IdMedico, request.IdPaciente, request.DataHora);
                return Created(string.Empty, new { message = "Consulta agendada com sucesso!" });
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "P0001")
                    return Conflict(new { message = ex.MessageText });
                if (ex.SqlState == "23503")
                    return BadRequest(new { message = "Médico ou Paciente informado não existe." });
                return StatusCode(500, new { message = "Erro no banco de dados.", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado.", details = ex.Message });
            }
        }

        /// <summary>Cancelar consulta</summary>
        /// <param name="id" example="1">ID da consulta a ser cancelada</param>
        /// <remarks>
        /// Cancela a consulta alterando o status para 'Cancelada'.
        /// A consulta **não é excluída** do banco — fica registrada como cancelada.
        /// </remarks>
        [HttpPut("{id:int}/cancelar")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var cancelado = await _repo.CancelarConsultaAsync(id);
                if (!cancelado)
                    return NotFound(new { message = $"Consulta com Id {id} não encontrada ou já estava cancelada." });
                return Ok(new { message = "Consulta cancelada com sucesso!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao cancelar consulta.", details = ex.Message });
            }
        }

        /// <summary>Listar consultas do dia</summary>
        /// <remarks>
        /// Retorna todas as consultas de uma data específica.
        /// O filtro por médico é opcional — se não informado, retorna consultas de todos os médicos.
        ///
        /// Exemplos de uso:
        ///
        ///     GET /api/consultas/diaria?data=2024-06-10
        ///     GET /api/consultas/diaria?data=2024-06-10&amp;idMedico=1
        ///
        /// </remarks>
        /// <param name="data" example="2024-06-10">Data das consultas (formato: AAAA-MM-DD)</param>
        /// <param name="idMedico" example="1">ID do médico (opcional — filtra por médico)</param>
        [HttpGet("diaria")]
        [ProducesResponseType(typeof(IEnumerable<Consulta>), 200)]
        public async Task<IActionResult> GetDiaria(
            [FromQuery] DateTime data,
            [FromQuery] int? idMedico = null)
        {
            try
            {
                var consultas = await _repo.GetConsultasDiariaAsync(data, idMedico);
                return Ok(consultas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao buscar consultas.", details = ex.Message });
            }
        }

        /// <summary>Estatísticas do médico — total de consultas ativas</summary>
        /// <param name="idMedico" example="1">ID do médico</param>
        /// <remarks>
        /// Chama a **Function** do PostgreSQL `qtd_consultas_por_medico` para retornar
        /// o total de consultas ativas (não canceladas) do médico informado.
        /// </remarks>
        [HttpGet("medico/{idMedico:int}/estatisticas")]
        [ProducesResponseType(typeof(EstatisticasMedicoResponse), 200)]
        public async Task<IActionResult> GetEstatisticasMedico(int idMedico)
        {
            try
            {
                var total = await _repo.GetQuantidadeConsultasPorMedicoAsync(idMedico);
                return Ok(new EstatisticasMedicoResponse
                {
                    IdMedico             = idMedico,
                    TotalConsultasAtivas = total
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao buscar estatísticas.", details = ex.Message });
            }
        }
    }
}
