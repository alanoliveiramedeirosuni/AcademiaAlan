-- Alan Medeiros -- Estrutura do banco para SQLite
CREATE TABLE IF NOT EXISTS tb_logradouro (
    id_logradouro INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    cep           TEXT    NOT NULL,
    nome          TEXT    NOT NULL,
    bairro        TEXT    NOT NULL,
    cidade        TEXT    NOT NULL,
    estado        TEXT    NOT NULL,
    pais          TEXT    NOT NULL
);
-- @@SPLIT@@
CREATE TABLE IF NOT EXISTS tb_colaborador (
    id_colaborador INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    cpf            TEXT    NOT NULL,
    nome           TEXT    NOT NULL,
    nascimento     TEXT    NOT NULL,
    telefone       TEXT    NOT NULL,
    email          TEXT    NOT NULL,
    logradouro_id  INTEGER NOT NULL,
    numero         TEXT    NOT NULL,
    complemento    TEXT    NULL,
    senha          TEXT    NOT NULL,
    foto           BLOB    NULL,
    admissao       TEXT    NOT NULL,
    tipo           INTEGER NOT NULL,
    vinculo        INTEGER NOT NULL,
    CONSTRAINT fk_colaborador_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro)
);
-- @@SPLIT@@
CREATE TABLE IF NOT EXISTS tb_aluno (
    id_aluno      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    cpf           TEXT    NOT NULL,
    nome          TEXT    NOT NULL,
    nascimento    TEXT    NOT NULL,
    telefone      TEXT    NOT NULL,
    email         TEXT    NOT NULL,
    logradouro_id INTEGER NOT NULL,
    numero        TEXT    NOT NULL,
    complemento   TEXT    NULL,
    senha         TEXT    NOT NULL,
    foto          BLOB    NULL,
    CONSTRAINT fk_aluno_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro)
);
-- @@SPLIT@@
CREATE TABLE IF NOT EXISTS tb_matricula (
    id_matricula     INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    aluno_id         INTEGER NOT NULL,
    plano            INTEGER NOT NULL,
    data_inicio      TEXT    NOT NULL,
    data_fim         TEXT    NOT NULL,
    objetivo         TEXT    NOT NULL,
    restricao_medica INTEGER NOT NULL,
    obs_restricao    TEXT    NULL,
    laudo_medico     BLOB    NULL,
    CONSTRAINT fk_matricula_aluno FOREIGN KEY (aluno_id)
        REFERENCES tb_aluno (id_aluno)
);
