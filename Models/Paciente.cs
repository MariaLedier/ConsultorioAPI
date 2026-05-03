namespace ConsultorioAPI.Models
{
    /// <summary>
    /// Representa um paciente cadastrado no sistema.
    /// Espelha a tabela Pacientes do PostgreSQL.
    /// </summary>
    public class Paciente
    {
        public int    IdPaciente { get; set; }
        public string Nome       { get; set; } = string.Empty;
        public string CPF        { get; set; } = string.Empty;
        public string? Telefone  { get; set; }
    }
}
