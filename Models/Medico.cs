namespace ConsultorioAPI.Models
{
    /// <summary>
    /// Representa um médico cadastrado no sistema.
    /// Espelha a tabela Medicos do PostgreSQL.
    /// </summary>
    public class Medico
    {
        public int    IdMedico      { get; set; }
        public string Nome          { get; set; } = string.Empty;
        public string CRM           { get; set; } = string.Empty;
        public string? Especialidade { get; set; }
    }
}
