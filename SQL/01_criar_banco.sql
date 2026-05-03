-- ============================================================
-- SISTEMA DE AGENDAMENTO DE CONSULTAS
-- Script SQL - Estratégia SQL First
-- Execute este script no pgAdmin 4 após criar o banco ConsultorioDB
-- ============================================================

-- ============================================================
-- PASSO 1: CRIAR AS TABELAS
-- ============================================================

-- Tabela de Médicos
CREATE TABLE IF NOT EXISTS Medicos (
    IdMedico    SERIAL PRIMARY KEY,
    Nome        VARCHAR(100) NOT NULL,
    CRM         VARCHAR(20)  UNIQUE NOT NULL,
    Especialidade VARCHAR(100)
);

-- Tabela de Pacientes
CREATE TABLE IF NOT EXISTS Pacientes (
    IdPaciente  SERIAL PRIMARY KEY,
    Nome        VARCHAR(100) NOT NULL,
    CPF         VARCHAR(14)  UNIQUE NOT NULL,
    Telefone    VARCHAR(20)
);

-- Tabela de Consultas
CREATE TABLE IF NOT EXISTS Consultas (
    IdConsulta  SERIAL PRIMARY KEY,
    IdMedico    INT          NOT NULL,
    IdPaciente  INT          NOT NULL,
    DataHora    TIMESTAMP    NOT NULL,
    Status      VARCHAR(50)  NOT NULL DEFAULT 'Agendada',
    FOREIGN KEY (IdMedico)   REFERENCES Medicos(IdMedico),
    FOREIGN KEY (IdPaciente) REFERENCES Pacientes(IdPaciente)
);

-- ============================================================
-- PASSO 2: CRIAR O ÍNDICE DE PERFORMANCE
-- Acelera buscas por médico + data (listagem diária, conflito de horário)
-- ============================================================

CREATE INDEX IF NOT EXISTS idx_consulta_data_medico
    ON Consultas(IdMedico, DataHora);

-- ============================================================
-- PASSO 3: TRIGGER — Impede duplicidade de horário (RN01 e RN02)
-- É a última linha de defesa: nenhuma aplicação burla esta regra
-- ============================================================

CREATE OR REPLACE FUNCTION check_duplicidade_horario()
RETURNS TRIGGER AS $$
BEGIN
    -- Verifica se já existe consulta ativa para o mesmo médico no mesmo horário
    -- O filtro Status != 'Cancelada' permite reusar horários de consultas canceladas
    IF EXISTS (
        SELECT 1
        FROM Consultas
        WHERE IdMedico = NEW.IdMedico
          AND DataHora  = NEW.DataHora
          AND Status   != 'Cancelada'
          AND IdConsulta != COALESCE(NEW.IdConsulta, -1) -- evita conflito consigo mesmo no UPDATE
    ) THEN
        RAISE EXCEPTION 'Conflito de horário: O médico já possui consulta neste horário.';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Remove a trigger se já existir antes de recriar
DROP TRIGGER IF EXISTS trg_impedir_duplicidade ON Consultas;

CREATE TRIGGER trg_impedir_duplicidade
    BEFORE INSERT OR UPDATE ON Consultas
    FOR EACH ROW EXECUTE FUNCTION check_duplicidade_horario();

-- ============================================================
-- PASSO 4: PROCEDURE — Agendar consulta
-- Encapsula a inserção; a Trigger faz a validação de conflito
-- ============================================================

CREATE OR REPLACE PROCEDURE agendar_consulta(
    p_id_medico   INT,
    p_id_paciente INT,
    p_data_hora   TIMESTAMP
)
LANGUAGE plpgsql AS $$
BEGIN
    -- A Trigger trg_impedir_duplicidade dispara automaticamente aqui.
    -- Se houver conflito, ela levanta EXCEPTION e o INSERT é cancelado.
    INSERT INTO Consultas (IdMedico, IdPaciente, DataHora, Status)
    VALUES (p_id_medico, p_id_paciente, p_data_hora, 'Agendada');
END;
$$;

-- ============================================================
-- PASSO 5: FUNCTION — Quantidade de consultas ativas por médico (RF06)
-- ============================================================

CREATE OR REPLACE FUNCTION qtd_consultas_por_medico(p_id_medico INT)
RETURNS INT AS $$
DECLARE
    total INT;
BEGIN
    SELECT COUNT(*)
      INTO total
      FROM Consultas
     WHERE IdMedico = p_id_medico
       AND Status  != 'Cancelada';

    RETURN total;
END;
$$ LANGUAGE plpgsql;

-- ============================================================
-- PASSO 6: DADOS DE TESTE (opcional — remova em produção)
-- ============================================================

INSERT INTO Medicos (Nome, CRM, Especialidade)
VALUES
    ('Dr. Carlos Silva',   'CRM-SP-12345', 'Cardiologia'),
    ('Dra. Ana Souza',     'CRM-RJ-67890', 'Dermatologia'),
    ('Dr. Pedro Almeida',  'CRM-MG-11223', 'Ortopedia')
ON CONFLICT DO NOTHING;

INSERT INTO Pacientes (Nome, CPF, Telefone)
VALUES
    ('Maria Oliveira',   '111.222.333-44', '(11) 99999-1111'),
    ('João Santos',      '555.666.777-88', '(11) 99999-2222'),
    ('Lucia Ferreira',   '999.000.111-22', '(11) 99999-3333')
ON CONFLICT DO NOTHING;
