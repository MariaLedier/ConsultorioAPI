namespace ConsultorioAPI.DTOs
{
    // ── Médico ────────────────────────────────────────────────────────────────

    /// <summary>Dados para cadastrar ou editar um médico.</summary>
    public class MedicoRequest
    {
        /// <example>Dr. Carlos Silva</example>
        public string  Nome          { get; set; } = string.Empty;

        /// <example>CRM-SP-12345</example>
        public string  CRM           { get; set; } = string.Empty;

        /// <example>Cardiologia</example>
        public string? Especialidade { get; set; }
    }

    // ── Paciente ──────────────────────────────────────────────────────────────

    /// <summary>Dados para cadastrar ou editar um paciente.</summary>
    public class PacienteRequest
    {
        /// <example>Maria Oliveira</example>
        public string  Nome     { get; set; } = string.Empty;

        /// <example>111.222.333-44</example>
        public string  CPF      { get; set; } = string.Empty;

        /// <example>(11) 99999-1111</example>
        public string? Telefone { get; set; }
    }

    // ── Consulta ──────────────────────────────────────────────────────────────

    /// <summary>Dados para agendar uma consulta.</summary>
    public class AgendamentoRequest
    {
        /// <example>1</example>
        public int      IdMedico   { get; set; }

        /// <example>1</example>
        public int      IdPaciente { get; set; }

        /// <example>2024-06-10T10:00:00</example>
        public DateTime DataHora   { get; set; }
    }

    /// <summary>Estatísticas de consultas de um médico.</summary>
    public class EstatisticasMedicoResponse
    {
        /// <example>1</example>
        public int IdMedico             { get; set; }

        /// <example>5</example>
        public int TotalConsultasAtivas { get; set; }
    }
}
