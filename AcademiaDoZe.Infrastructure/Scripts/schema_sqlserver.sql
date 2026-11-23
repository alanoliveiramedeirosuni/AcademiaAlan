-- Alan Medeiros -- Estrutura do banco para SQL Server
IF OBJECT_ID('tb_logradouro', 'U') IS NULL
CREATE TABLE tb_logradouro (
    id_logradouro INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    cep           VARCHAR(8)   NOT NULL,
    nome          VARCHAR(100) NOT NULL,
    bairro        VARCHAR(100) NOT NULL,
    cidade        VARCHAR(100) NOT NULL,
    estado        VARCHAR(2)   NOT NULL,
    pais          VARCHAR(100) NOT NULL
);
-- @@SPLIT@@
IF OBJECT_ID('tb_colaborador', 'U') IS NULL
CREATE TABLE tb_colaborador (
    id_colaborador INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    cpf            VARCHAR(11)  NOT NULL,
    nome           VARCHAR(100) NOT NULL,
    nascimento     DATE         NOT NULL,
    telefone       VARCHAR(11)  NOT NULL,
    email          VARCHAR(100) NOT NULL,
    logradouro_id  INT          NOT NULL,
    numero         VARCHAR(10)  NOT NULL,
    complemento    VARCHAR(50)  NULL,
    senha          VARCHAR(255) NOT NULL,
    foto           VARBINARY(MAX) NULL,
    admissao       DATE         NOT NULL,
    tipo           INT          NOT NULL,
    vinculo        INT          NOT NULL,
    CONSTRAINT fk_colaborador_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro)
);
-- @@SPLIT@@
IF OBJECT_ID('tb_aluno', 'U') IS NULL
CREATE TABLE tb_aluno (
    id_aluno      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    cpf           VARCHAR(11)  NOT NULL,
    nome          VARCHAR(100) NOT NULL,
    nascimento    DATE         NOT NULL,
    telefone      VARCHAR(11)  NOT NULL,
    email         VARCHAR(100) NOT NULL,
    logradouro_id INT          NOT NULL,
    numero        VARCHAR(10)  NOT NULL,
    complemento   VARCHAR(50)  NULL,
    senha         VARCHAR(255) NOT NULL,
    foto          VARBINARY(MAX) NULL,
    CONSTRAINT fk_aluno_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro)
);
-- @@SPLIT@@
IF OBJECT_ID('tb_matricula', 'U') IS NULL
CREATE TABLE tb_matricula (
    id_matricula     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    aluno_id         INT           NOT NULL,
    plano            INT           NOT NULL,
    data_inicio      DATE          NOT NULL,
    data_fim         DATE          NOT NULL,
    objetivo         VARCHAR(200)  NOT NULL,
    restricao_medica INT           NOT NULL,
    obs_restricao    VARCHAR(200)  NULL,
    laudo_medico     VARBINARY(MAX) NULL,
    CONSTRAINT fk_matricula_aluno FOREIGN KEY (aluno_id)
        REFERENCES tb_aluno (id_aluno)
);

-- @@SPLIT@@

IF OBJECT_ID('tb_registro_acesso', 'U') IS NULL
BEGIN
    CREATE TABLE tb_registro_acesso (
        id_acesso INT IDENTITY(1,1) PRIMARY KEY,
        tipo INT NOT NULL,
        pessoa_id INT NOT NULL,
        entrada DATETIME2 NOT NULL,
        saida DATETIME2 NULL,
        pessoa_aberta INT NULL,
        CONSTRAINT ck_acesso_tipo CHECK (tipo IN (0, 1)),
        CONSTRAINT ck_acesso_horarios CHECK (saida IS NULL OR saida >= entrada)
    );
    CREATE UNIQUE INDEX ux_acesso_aberto ON tb_registro_acesso(tipo, pessoa_aberta) WHERE pessoa_aberta IS NOT NULL;
END;
