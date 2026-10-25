using System.Text;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Caminho do arquivo .db (por padrao, "quiz.db" na pasta da API)
string caminhoBanco = builder.Configuration["Banco"] ?? "quiz.db";
string conexao = $"Data Source={caminhoBanco}";

PrepararBanco(conexao);

// Qualquer erro inesperado vira uma resposta JSON, sem derrubar a API
app.UseExceptionHandler(erro => erro.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { erro = "Erro ao acessar o banco de dados." });
}));

// ---------- GET /api/marcas ----------
app.MapGet("/api/marcas", () =>
{
    using var cn = Abrir();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "SELECT Id, NomeMarca, Descricao FROM Marcas WHERE Ativo = 1 ORDER BY NomeMarca";

    var lista = new List<object>();
    using var r = cmd.ExecuteReader();
    while (r.Read())
        lista.Add(new { id = r.GetInt32(0), nome = r.GetString(1), descricao = r.GetString(2) });

    return Results.Ok(lista);
});

// ---------- GET /api/perguntas?quantidade=10&dificuldade=Fácil&marcaId=1&excluir=3,7 ----------
app.MapGet("/api/perguntas", (int? quantidade, string? dificuldade, int? marcaId, string? excluir) =>
{
    int limite = Math.Clamp(quantidade ?? 10, 1, 50);

    var sql = new StringBuilder(@"
        SELECT p.Id, p.Enunciado, p.AlternativaA, p.AlternativaB, p.AlternativaC, p.AlternativaD,
               p.RespostaCorreta, p.Dificuldade, p.Pista, p.Feedback, m.NomeMarca, m.Descricao
        FROM Perguntas p
        LEFT JOIN Marcas m ON m.Id = p.MarcaId
        WHERE p.Ativo = 1");

    using var cn = Abrir();
    using var cmd = cn.CreateCommand();

    if (!string.IsNullOrWhiteSpace(dificuldade))
    {
        sql.Append(" AND p.Dificuldade = $dif");
        cmd.Parameters.AddWithValue("$dif", dificuldade);
    }

    if (marcaId.HasValue)
    {
        sql.Append(" AND p.MarcaId = $marca");
        cmd.Parameters.AddWithValue("$marca", marcaId.Value);
    }

    // ids que o jogo ja usou nesta partida (evita repeticao)
    var ids = new List<int>();
    foreach (var parte in (excluir ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (int.TryParse(parte, out int n)) ids.Add(n);
    }

    if (ids.Count > 0)
    {
        var nomes = new List<string>();
        for (int i = 0; i < ids.Count; i++)
        {
            nomes.Add("$e" + i);
            cmd.Parameters.AddWithValue("$e" + i, ids[i]);
        }
        sql.Append(" AND p.Id NOT IN (" + string.Join(",", nomes) + ")");
    }

    sql.Append(" ORDER BY RANDOM() LIMIT $limite");
    cmd.Parameters.AddWithValue("$limite", limite);
    cmd.CommandText = sql.ToString();

    var lista = new List<object>();
    using var r = cmd.ExecuteReader();
    while (r.Read())
    {
        lista.Add(new
        {
            id = r.GetInt32(0),
            enunciado = r.GetString(1),
            alternativas = new[]
            {
                new { letra = "A", texto = r.GetString(2) },
                new { letra = "B", texto = r.GetString(3) },
                new { letra = "C", texto = r.GetString(4) },
                new { letra = "D", texto = r.GetString(5) }
            },
            respostaCorreta = r.GetString(6),
            dificuldade = r.GetString(7),
            pista = r.IsDBNull(8) ? null : r.GetString(8),
            feedback = r.GetString(9),
            marca = r.IsDBNull(10) ? null : r.GetString(10),
            marcaDescricao = r.IsDBNull(11) ? null : r.GetString(11)
        });
    }

    return Results.Ok(lista);
});

// ---------- POST /api/partidas ----------
app.MapPost("/api/partidas", (PartidaDto p) =>
{
    if (p.Pontuacao < 0)
        return Results.BadRequest(new { erro = "Pontuação inválida." });

    string nick = string.IsNullOrWhiteSpace(p.Nickname) ? "Anônimo" : p.Nickname.Trim();
    string modo = string.IsNullOrWhiteSpace(p.Modo) ? "Quiz" : p.Modo.Trim();

    using var cn = Abrir();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Partidas (Nickname, Pontuacao, Modo, TotalQuestoes, TotalAcertos, TempoSegundos, DicasUsadas, DataHora)
        VALUES ($nick, $pont, $modo, $q, $a, $t, $d, datetime('now', 'localtime'));
        SELECT last_insert_rowid();";
    cmd.Parameters.AddWithValue("$nick", nick);
    cmd.Parameters.AddWithValue("$pont", p.Pontuacao);
    cmd.Parameters.AddWithValue("$modo", modo);
    cmd.Parameters.AddWithValue("$q", p.TotalQuestoes);
    cmd.Parameters.AddWithValue("$a", p.TotalAcertos);
    cmd.Parameters.AddWithValue("$t", p.TempoPartida);
    cmd.Parameters.AddWithValue("$d", p.DicasUsadas);

    long id = (long)cmd.ExecuteScalar()!;
    return Results.Created($"/api/partidas/{id}", new { id, mensagem = "Partida registrada." });
});

// ---------- GET /api/partidas?limite=20 (historico) ----------
app.MapGet("/api/partidas", (int? limite) =>
{
    using var cn = Abrir();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
        SELECT Id, Nickname, Pontuacao, Modo, TotalQuestoes, TotalAcertos, TempoSegundos, DicasUsadas, DataHora
        FROM Partidas
        ORDER BY DataHora DESC, Id DESC
        LIMIT $limite";
    cmd.Parameters.AddWithValue("$limite", Math.Clamp(limite ?? 20, 1, 200));

    var lista = new List<object>();
    using var r = cmd.ExecuteReader();
    while (r.Read())
    {
        lista.Add(new
        {
            id = r.GetInt32(0),
            nickname = r.GetString(1),
            pontuacao = r.GetInt32(2),
            modo = r.GetString(3),
            totalQuestoes = r.GetInt32(4),
            totalAcertos = r.GetInt32(5),
            tempoSegundos = r.GetInt32(6),
            dicasUsadas = r.GetInt32(7),
            dataHora = r.IsDBNull(8) ? null : r.GetString(8)
        });
    }

    return Results.Ok(lista);
});

app.Run("http://localhost:5000");

// =====================================================================
// Funcoes auxiliares
// =====================================================================

SqliteConnection Abrir()
{
    var cn = new SqliteConnection(conexao);
    cn.Open();
    return cn;
}

// Confere se as tabelas existem e acrescenta as colunas que faltam (so se ainda nao existirem)
void PrepararBanco(string cx)
{
    using var cn = new SqliteConnection(cx);
    cn.Open();

    using var checa = cn.CreateCommand();
    checa.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Marcas', 'Perguntas', 'Partidas')";
    if ((long)checa.ExecuteScalar()! < 3)
    {
        Console.WriteLine("Banco sem as tabelas esperadas (Marcas, Perguntas, Partidas).");
        Console.WriteLine("Rode o script .sql do banco para criar o arquivo e tente de novo.");
        Environment.Exit(1);
    }

    // Permite inativar conteudo sem apagar
    AdicionarColuna(cn, "Marcas", "Ativo", "INTEGER NOT NULL DEFAULT 1");
    AdicionarColuna(cn, "Perguntas", "Ativo", "INTEGER NOT NULL DEFAULT 1");

    // Metricas do historico de partidas
    AdicionarColuna(cn, "Partidas", "Modo", "TEXT NOT NULL DEFAULT 'Quiz'");
    AdicionarColuna(cn, "Partidas", "TotalQuestoes", "INTEGER NOT NULL DEFAULT 0");
    AdicionarColuna(cn, "Partidas", "TotalAcertos", "INTEGER NOT NULL DEFAULT 0");
    AdicionarColuna(cn, "Partidas", "TempoSegundos", "INTEGER NOT NULL DEFAULT 0");
    AdicionarColuna(cn, "Partidas", "DicasUsadas", "INTEGER NOT NULL DEFAULT 0");
}

void AdicionarColuna(SqliteConnection cn, string tabela, string coluna, string definicao)
{
    bool existe = false;

    using (var info = cn.CreateCommand())
    {
        info.CommandText = $"PRAGMA table_info({tabela})";
        using var r = info.ExecuteReader();
        while (r.Read())
        {
            if (r.GetString(1) == coluna) { existe = true; break; }
        }
    }

    if (existe) return;

    using var alter = cn.CreateCommand();
    alter.CommandText = $"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao}";
    alter.ExecuteNonQuery();
}

// Corpo do POST /api/partidas (os nomes batem com o ResultadoPartida do frontend)
record PartidaDto(
    string? Nickname,
    int Pontuacao,
    string? Modo,
    int TotalQuestoes,
    int TotalAcertos,
    int TempoPartida,
    int DicasUsadas);
