using ConsultorioAPI.DTOs;
using ConsultorioAPI.Models;
using ConsultorioAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ConsultorioAPI.Controllers
{
    [ApiController]
    [Route("api/medicos")]
    [Produces("application/json")]
    // ── TAG: agrupa todos os endpoints deste controller sob "Médicos" no Swagger
    [Tags("Médicos")]
    public class MedicosController : ControllerBase
    {
        private readonly MedicoRepository _repo;
        public MedicosController(MedicoRepository repo) => _repo = repo;

        /// <summary>Listar todos os médicos</summary>
        /// <remarks>Retorna a lista completa de médicos cadastrados, ordenada por nome.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Medico>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var medicos = await _repo.GetAllAsync();
            return Ok(medicos);
        }

        /// <summary>Buscar médico por ID</summary>
        /// <param name="id" example="1">ID do médico</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Medico), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var medico = await _repo.GetByIdAsync(id);
            if (medico is null)
                return NotFound(new { message = $"Médico com Id {id} não encontrado." });
            return Ok(medico);
        }

        /// <summary>Cadastrar novo médico</summary>
        /// <remarks>
        /// Exemplo de requisição:
        ///
        ///     POST /api/medicos
        ///     {
        ///         "nome": "Dr. Carlos Silva",
        ///         "crm": "CRM-SP-12345",
        ///         "especialidade": "Cardiologia"
        ///     }
        ///
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] MedicoRequest request)
        {
            try
            {
                var idGerado = await _repo.AddAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = idGerado },
                    new { IdMedico = idGerado, message = "Médico cadastrado com sucesso!" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new { message = "Já existe um médico cadastrado com este CRM." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao cadastrar médico.", details = ex.Message });
            }
        }

        /// <summary>Alterar dados do médico</summary>
        /// <param name="id" example="1">ID do médico a ser alterado</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, [FromBody] MedicoRequest request)
        {
            try
            {
                var atualizado = await _repo.UpdateAsync(id, request);
                if (!atualizado)
                    return NotFound(new { message = $"Médico com Id {id} não encontrado." });
                return Ok(new { message = "Médico atualizado com sucesso!" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new { message = "Já existe um médico cadastrado com este CRM." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao alterar médico.", details = ex.Message });
            }
        }

        /// <summary>Excluir médico</summary>
        /// <param name="id" example="1">ID do médico a ser excluído</param>
        /// <remarks>Não é possível excluir um médico que possui consultas vinculadas.</remarks>
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
                    return NotFound(new { message = $"Médico com Id {id} não encontrado." });
                return Ok(new { message = "Médico excluído com sucesso!" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Conflict(new { message = "Não é possível excluir este médico pois existem consultas vinculadas a ele." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao excluir médico.", details = ex.Message });
            }
        }
    }
}
