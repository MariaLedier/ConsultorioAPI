namespace ConsultorioAPI.Models
{
    /// <summary>
    /// Representa uma consulta agendada.
    /// Espelha a tabela Consultas do PostgreSQL.
    /// </summary>
    public class Consulta
    {
        public int      IdConsulta  { get; set; }
        public int      IdMedico    { get; set; }
        public int      IdPaciente  { get; set; }
        public DateTime DataHora    { get; set; }
        public string   Status      { get; set; } = "Agendada";

        // Propriedades de navegação (preenchidas em joins — não existem no banco)
        public string? NomeMedico   { get; set; }
        public string? NomePaciente { get; set; }
    }
}
