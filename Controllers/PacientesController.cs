using ConsultorioAPI.DTOs;
using ConsultorioAPI.Models;
using ConsultorioAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ConsultorioAPI.Controllers
{
    [ApiController]
    [Route("api/pacientes")]
    [Produces("application/json")]
    [Tags("Pacientes")]
    public class PacientesController : ControllerBase
    {
        private readonly PacienteRepository _repo;
        public PacientesController(PacienteRepository repo) => _repo = repo;

        /// <summary>Listar todos os pacientes</summary>
        /// <remarks>Retorna a lista completa de pacientes cadastrados, ordenada por nome.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Paciente>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var pacientes = await _repo.GetAllAsync();
            return Ok(pacientes);
        }

        /// <summary>Buscar paciente por ID</summary>
        /// <param name="id" example="1">ID do paciente</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Paciente), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var paciente = await _repo.GetByIdAsync(id);
            if (paciente is null)
                return NotFound(new { message = $"Paciente com Id {id} não encontrado." });
            return Ok(paciente);
        }

        /// <summary>Cadastrar novo paciente</summary>
        /// <remarks>
        /// Exemplo de requisição:
        ///
        ///     POST /api/pacientes
        ///     {
        ///         "nome": "Maria Oliveira",
        ///         "cpf": "111.222.333-44",
        ///         "telefone": "(11) 99999-1111"
        ///     }
        ///
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] PacienteRequest request)
        {
            try
            {
                var idGerado = await _repo.AddAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = idGerado },
                    new { IdPaciente = idGerado, message = "Paciente cadastrado com sucesso!" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new { message = "Já existe um paciente cadastrado com este CPF." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao cadastrar paciente.", details = ex.Message });
            }
        }

        /// <summary>Alterar dados do paciente</summary>
        /// <param name="id" example="1">ID do paciente a ser alterado</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, [FromBody] PacienteRequest request)
        {
            try
            {
                var atualizado = await _repo.UpdateAsync(id, request);
                if (!atualizado)
                    return NotFound(new { message = $"Paciente com Id {id} não encontrado." });
                return Ok(new { message = "Paciente atualizado com sucesso!" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new { message = "Já existe um paciente cadastrado com este CPF." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao alterar paciente.", details = ex.Message });
            }
        }

        /// <summary>Excluir paciente</summary>
        /// <param name="id" example="1">ID do paciente a ser excluído</param>
        /// <remarks>Não é possível excluir um paciente que possui consultas vinculadas.</remarks>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var removido = await _repo.DeleteAsync(id);
                if (!removido)
                    return NotFound(new { message = $"Paciente com Id {id} não encontrado." });
                return Ok(new { message = "Paciente excluído com sucesso!" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Conflict(new { message = "Não é possível excluir este paciente pois existem consultas vinculadas a ele." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao excluir paciente.", details = ex.Message });
            }
        }
    }
}
