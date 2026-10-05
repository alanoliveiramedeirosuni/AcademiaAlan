-- Alan Medeiros -- Estrutura do banco para MySQL
CREATE TABLE IF NOT EXISTS tb_logradouro (
    id_logradouro INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    cep           VARCHAR(8)   NOT NULL,
    nome          VARCHAR(100) NOT NULL,
    bairro        VARCHAR(100) NOT NULL,
    cidade        VARCHAR(100) NOT NULL,
    estado        VARCHAR(2)   NOT NULL,
    pais          VARCHAR(100) NOT NULL
);
-- @@SPLIT@@
CREATE TABLE IF NOT EXISTS tb_colaborador (
    id_colaborador INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    cpf            VARCHAR(11)  NOT NULL,
    nome           VARCHAR(100) NOT NULL,
    nascimento     DATE         NOT NULL,
    telefone       VARCHAR(11)  NOT NULL,
    email          VARCHAR(100) NOT NULL,
    logradouro_id  INT          NOT NULL,
    numero         VARCHAR(10)  NOT NULL,
    complemento    VARCHAR(50)  NULL,
    senha          VARCHAR(255) NOT NULL,
    foto           LONGBLOB     NULL,
    admissao       DATE         NOT NULL,
    tipo           INT          NOT NULL,
    vinculo        INT          NOT NULL,
    CONSTRAINT fk_colaborador_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro)
);
-- @@SPLIT@@
CREATE TABLE IF NOT EXISTS tb_aluno (
    id_aluno      INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    cpf           VARCHAR(11)  NOT NULL,
    nome          VARCHAR(100) NOT NULL,
    nascimento    DATE         NOT NULL,
    telefone      VARCHAR(11)  NOT NULL,
    email         VARCHAR(100) NOT NULL,
    logradouro_id INT          NOT NULL,
    numero        VARCHAR(10)  NOT NULL,
    complemento   VARCHAR(50)  NULL,
    senha         VARCHAR(255) NOT NULL,
    foto          LONGBLOB     NULL,
    CONSTRAINT fk_aluno_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro)
);
-- @@SPLIT@@
CREATE TABLE IF NOT EXISTS tb_matricula (
    id_matricula     INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    aluno_id         INT          NOT NULL,
    plano            INT          NOT NULL,
    data_inicio      DATE         NOT NULL,
    data_fim         DATE         NOT NULL,
    objetivo         VARCHAR(200) NOT NULL,
    restricao_medica INT          NOT NULL,
    obs_restricao    VARCHAR(200) NULL,
    laudo_medico     LONGBLOB     NULL,
    CONSTRAINT fk_matricula_aluno FOREIGN KEY (aluno_id)
        REFERENCES tb_aluno (id_aluno)
);
