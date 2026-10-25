using System.Globalization;
using System.Net.Http.Json;
using System.Text;

namespace QuizArcor;

// =====================================================================
// Modelos que chegam da API (nomes batem com o JSON de /api/perguntas)
// =====================================================================
public class Alternativa
{
    public string Letra { get; set; } = "";
    public string Texto { get; set; } = "";
}

public class Pergunta
{
    public int Id { get; set; }
    public string Enunciado { get; set; } = "";
    public List<Alternativa> Alternativas { get; set; } = new();
    public string RespostaCorreta { get; set; } = "";
    public string Dificuldade { get; set; } = "";
    public string? Pista { get; set; }
    public string Feedback { get; set; } = "";
    public string? Marca { get; set; }
    public string? MarcaDescricao { get; set; }
}

// Linha do historico (GET /api/partidas)
public class PartidaHistorico
{
    public int Id { get; set; }
    public string Nickname { get; set; } = "";
    public int Pontuacao { get; set; }
    public string Modo { get; set; } = "";
    public int TotalQuestoes { get; set; }
    public int TotalAcertos { get; set; }
    public int TempoSegundos { get; set; }
    public int DicasUsadas { get; set; }
    public string? DataHora { get; set; }
}

// =====================================================================
// Dados e resultado da partida (logica da entrega 1)
// =====================================================================
public class DadosPartida
{
    public string Nickname { get; set; } = "";
    public int FaixaEtaria { get; set; }          // 1 a 6 (6 = prefiro nao informar)
    public int QuestoesFaceis { get; set; }
    public int AcertosFaceis { get; set; }
    public int QuestoesMedias { get; set; }
    public int AcertosMedias { get; set; }
    public int QuestoesDificeis { get; set; }
    public int AcertosDificeis { get; set; }
    public int TempoPartida { get; set; }         // segundos
    public int DicasUsadas { get; set; }
}

// E o que vai no JSON do POST /api/partidas
public class ResultadoPartida
{
    public string Nickname { get; set; } = "";
    public string Modo { get; set; } = "Quiz";
    public string NomeFaixa { get; set; } = "";
    public int TotalQuestoes { get; set; }
    public int TotalAcertos { get; set; }
    public int TotalErros { get; set; }
    public double PercentualGeral { get; set; }
    public double PctFacil { get; set; }
    public double PctMedio { get; set; }
    public double PctDificil { get; set; }
    public int Pontuacao { get; set; }
    public int PontuacaoMaxima { get; set; }
    public double AproveitamentoPontuacao { get; set; }
    public int DicasUsadas { get; set; }
    public int PenalidadeDicas { get; set; }
    public int TempoPartida { get; set; }
    public double TempoMedioPorQuestao { get; set; }
    public string Ritmo { get; set; } = "";
    public string MelhorNivel { get; set; } = "";
    public string Classificacao { get; set; } = "";
}

public static class CalculadoraPartida
{
    private static readonly string[] Faixas =
    {
        "Até 12 anos", "13 a 17 anos", "18 a 24 anos",
        "25 a 39 anos", "40 anos ou mais", "Prefiro não informar"
    };

    // Converte o texto do banco ("Fácil", "Médio", "Difícil") em 1, 2 ou 3
    public static int Nivel(string dificuldade)
    {
        var sb = new StringBuilder();
        foreach (char c in (dificuldade ?? "").Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string s = sb.ToString().Trim().ToLowerInvariant();
        if (s.StartsWith("m")) return 2;
        if (s.StartsWith("d")) return 3;
        return 1;
    }

    public static List<string> Validar(DadosPartida d)
    {
        var erros = new List<string>();
        int total = d.QuestoesFaceis + d.QuestoesMedias + d.QuestoesDificeis;

        if (string.IsNullOrWhiteSpace(d.Nickname)) erros.Add("Nickname ausente.");
        if (d.FaixaEtaria < 1 || d.FaixaEtaria > 6) erros.Add("Faixa etária inválida.");
        if (d.AcertosFaceis < 0 || d.AcertosFaceis > d.QuestoesFaceis) erros.Add("Acertos nas fáceis inválidos.");
        if (d.AcertosMedias < 0 || d.AcertosMedias > d.QuestoesMedias) erros.Add("Acertos nas médias inválidos.");
        if (d.AcertosDificeis < 0 || d.AcertosDificeis > d.QuestoesDificeis) erros.Add("Acertos nas difíceis inválidos.");
        if (total < 1) erros.Add("A soma das questões dos três níveis deve ser pelo menos 1.");
        if (d.TempoPartida <= 0) erros.Add("Tempo da partida inválido.");
        if (d.DicasUsadas < 0 || d.DicasUsadas > total) erros.Add("Quantidade de dicas inválida.");

        return erros;
    }

    // So chame depois de Validar() retornar lista vazia
    public static ResultadoPartida Calcular(DadosPartida d)
    {
        int total = d.QuestoesFaceis + d.QuestoesMedias + d.QuestoesDificeis;
        int acertos = d.AcertosFaceis + d.AcertosMedias + d.AcertosDificeis;

        double pctFacil = d.QuestoesFaceis > 0 ? d.AcertosFaceis * 100.0 / d.QuestoesFaceis : 0;
        double pctMedio = d.QuestoesMedias > 0 ? d.AcertosMedias * 100.0 / d.QuestoesMedias : 0;
        double pctDificil = d.QuestoesDificeis > 0 ? d.AcertosDificeis * 100.0 / d.QuestoesDificeis : 0;
        double pctGeral = acertos * 100.0 / total;

        int penalidade = d.DicasUsadas * 5;
        int pontuacao = Math.Max(0,
            d.AcertosFaceis * 10 + d.AcertosMedias * 20 + d.AcertosDificeis * 30 - penalidade);
        int pontuacaoMax = d.QuestoesFaceis * 10 + d.QuestoesMedias * 20 + d.QuestoesDificeis * 30;
        double tempoMedio = d.TempoPartida * 1.0 / total;

        string classificacao = pctGeral >= 90 ? "Mestre das Marcas"
                             : pctGeral >= 70 ? "Conhecedor de Marcas"
                             : pctGeral >= 50 ? "Aprendiz"
                             : "Iniciante";

        string ritmo = tempoMedio <= 10 ? "Rápido"
                     : tempoMedio <= 20 ? "Normal"
                     : "Pausado";

        string melhorNivel = "Nenhum";
        if (d.QuestoesDificeis > 0
            && (d.QuestoesMedias == 0 || pctDificil >= pctMedio)
            && (d.QuestoesFaceis == 0 || pctDificil >= pctFacil))
            melhorNivel = "Difícil";
        else if (d.QuestoesMedias > 0 && (d.QuestoesFaceis == 0 || pctMedio >= pctFacil))
            melhorNivel = "Médio";
        else if (d.QuestoesFaceis > 0)
            melhorNivel = "Fácil";

        return new ResultadoPartida
        {
            Nickname = d.Nickname,
            Modo = "Quiz",
            NomeFaixa = Faixas[d.FaixaEtaria - 1],
            TotalQuestoes = total,
            TotalAcertos = acertos,
            TotalErros = total - acertos,
            PercentualGeral = pctGeral,
            PctFacil = pctFacil,
            PctMedio = pctMedio,
            PctDificil = pctDificil,
            Pontuacao = pontuacao,
            PontuacaoMaxima = pontuacaoMax,
            AproveitamentoPontuacao = pontuacaoMax > 0 ? pontuacao * 100.0 / pontuacaoMax : 0,
            DicasUsadas = d.DicasUsadas,
            PenalidadeDicas = penalidade,
            TempoPartida = d.TempoPartida,
            TempoMedioPorQuestao = tempoMedio,
            Ritmo = ritmo,
            MelhorNivel = melhorNivel,
            Classificacao = classificacao
        };
    }
}

// =====================================================================
// Chamadas JSON ao servidor (a API do Quiz Arcor)
// =====================================================================
public class QuizApiService
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("http://localhost:5000/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    // GET: sorteia perguntas (dificuldade opcional: "Fácil", "Médio" ou "Difícil")
    public async Task<List<Pergunta>> ObterPerguntasAsync(string? dificuldade, int quantidade)
    {
        string url = $"api/perguntas?quantidade={quantidade}";
        if (!string.IsNullOrWhiteSpace(dificuldade))
            url += "&dificuldade=" + Uri.EscapeDataString(dificuldade);

        return await _http.GetFromJsonAsync<List<Pergunta>>(url) ?? new List<Pergunta>();
    }

    // POST: grava a partida no historico
    public async Task EnviarResultadoAsync(ResultadoPartida resultado)
    {
        var resposta = await _http.PostAsJsonAsync("api/partidas", resultado);
        resposta.EnsureSuccessStatusCode();
    }

    // GET: ultimas partidas
    public async Task<List<PartidaHistorico>> ObterHistoricoAsync()
    {
        return await _http.GetFromJsonAsync<List<PartidaHistorico>>("api/partidas?limite=20")
               ?? new List<PartidaHistorico>();
    }
}
