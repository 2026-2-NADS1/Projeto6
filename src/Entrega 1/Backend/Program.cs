String linha;
bool conversaoOk;

Console.Clear();

Console.WriteLine("Digite seu nickname:");
String nickname = Console.ReadLine();
if (nickname == null || nickname == "") {
    Console.WriteLine("ERRO na entrada 1 (nickname): dado ausente (ou fim do arquivo).");
    Environment.Exit(1);
}

Console.WriteLine("Digite faixa etaria:");
linha = Console.ReadLine();
int faixaEtaria;
conversaoOk = int.TryParse(linha, out faixaEtaria);
if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 2 (faixa etaria): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}
if(faixaEtaria < 1 || faixaEtaria > 6) {
    Console.WriteLine("ERRO na entrada 2 (faixa etaria): faixa inválida.");
    Environment.Exit(1);    
}
String nomeFaixa = "Até 12 anos";
if(faixaEtaria == 2) {
    nomeFaixa = "13 a 17 anos";
}
if(faixaEtaria == 3) {
    nomeFaixa = "18 a 24 anos";
}
if (faixaEtaria == 4)
{
    nomeFaixa = "25 a 39 anos";
}
if(faixaEtaria == 5) {
    nomeFaixa = "40 anos ou mais";
}
if (faixaEtaria == 6)
{
    nomeFaixa = "Prefiro não informar";
}

Console.WriteLine("Digite quantas questões fáceis foram apresentadas:");
linha = Console.ReadLine();
int questoesFaceis;
conversaoOk = int.TryParse(linha, out questoesFaceis);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 3 (questões fáceis apresentadas): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if(questoesFaceis < 0) {
    Console.WriteLine("ERRO na entrada 3 (questões fáceis apresentadas): quantidade inválida.");
    Environment.Exit(1);
}


Console.WriteLine("Digite quantas questões fáceis você acertou:");
linha = Console.ReadLine();
int acertosFaceis;
conversaoOk = int.TryParse(linha, out acertosFaceis);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 4 (acertos nas fáceis): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if (acertosFaceis < 0 || acertosFaceis > questoesFaceis)
{
    Console.WriteLine("ERRO na entrada 4 (acertos nas fáceis): quantidade inválida.");
    Environment.Exit(1);
}


Console.WriteLine("Digite quantas questões medias foram apresentadas:");
linha = Console.ReadLine();
int questoesMedias;
conversaoOk = int.TryParse(linha, out questoesMedias);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 5 (questões medias apresentadas): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}


if(questoesMedias < 0) {
    Console.WriteLine("ERRO na entrada 5 (questões medias apresentadas): quantidade inválida.");
    Environment.Exit(1);
}
Console.WriteLine("Digite quantas questões medias você acertou:");
linha = Console.ReadLine();
int acertosmMedias;
conversaoOk = int.TryParse(linha, out acertosmMedias);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 6 (acertos nas médias): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if(acertosmMedias < 0 || acertosmMedias > questoesMedias) {
    Console.WriteLine("ERRO na entrada 6 (acertos nas médias): quantidade inválida.");
    Environment.Exit(1);
}

Console.WriteLine("Digite quantas questões difíceis foram apresentadas:");
linha = Console.ReadLine();
int questoesDificeis;
conversaoOk = int.TryParse(linha, out questoesDificeis);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 7 (questões difíceis apresentadas): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if(questoesDificeis < 0) {
    Console.WriteLine("ERRO na entrada 7 (questões difíceis apresentadas): quantidade inválida.");
    Environment.Exit(1);
}

Console.WriteLine("Digite quantas questões difíceis você acertou:");
linha = Console.ReadLine();
int acertosDificeis;
conversaoOk = int.TryParse(linha, out acertosDificeis);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 8 (acertos nas difíceis): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if(acertosDificeis < 0 || acertosDificeis > questoesDificeis) {
    Console.WriteLine("ERRO na entrada 8 (acertos nas difíceis): quantidade inválida.");
    Environment.Exit(1);
}

int totalQuestoes = questoesFaceis + questoesMedias + questoesDificeis;
if (totalQuestoes < 1) {
    Console.WriteLine("ERRO no total de questões: a soma das questões dos três níveis deve ser pelo menos 1.");
    Environment.Exit(1);
}


Console.WriteLine("Digite o tempo total da partida, em segundos:");
linha = Console.ReadLine();
int tempoPartida;

conversaoOk = int.TryParse(linha, out tempoPartida);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 9 (tempo total da partida): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if(tempoPartida <= 0) {
    Console.WriteLine("ERRO na entrada 9 (tempo total da partida): tempo inválido.");
    Environment.Exit(1);
}

Console.WriteLine("Digite quantas dicas foram usadas na partida:");
linha = Console.ReadLine();
int dicasUsadas;

conversaoOk = int.TryParse(linha, out dicasUsadas);

if(!conversaoOk) {
    Console.WriteLine("ERRO na entrada 10 (dicas usadas na partida): dado ausente/inválido (ou fim do arquivo).");
    Environment.Exit(1);
}

if(dicasUsadas < 0 || dicasUsadas > totalQuestoes) {
    Console.WriteLine("ERRO na entrada 10 (dicas usadas na partida): quantidade inválida.");
    Environment.Exit(1);
}


totalQuestoes = questoesFaceis + questoesMedias + questoesDificeis;
int totalAcertos = acertosFaceis + acertosmMedias + acertosDificeis;
int totalErros = totalQuestoes - totalAcertos;


double percentualGeral = totalAcertos * 100.0 / totalQuestoes;

double pctFaacil = 0.0;
double pctMedio = 0.0;
double pctDificil = 0.0;

if (questoesFaceis > 0) {
    pctFaacil = acertosFaceis * 100.0 / questoesFaceis;
}

if (questoesMedias > 0) {
    pctMedio = acertosmMedias * 100.0 / questoesMedias;
}

if (questoesDificeis > 0) {
    pctDificil = acertosDificeis * 100.0 / questoesDificeis;
}


int penalidadeDicas = dicasUsadas * 5;
int pontuacao = (acertosFaceis * 10) + (acertosmMedias * 20) + (acertosDificeis * 30) - penalidadeDicas;
if (pontuacao < 0) {
    pontuacao = 0; 
}

int pontuacaoMaxima = (questoesFaceis * 10) + (questoesMedias * 20) + (questoesDificeis * 30);
double aproveitamentoPontuacao = pontuacao * 100.0 / pontuacaoMaxima;


int minutos = tempoPartida / 60;
int segundosRestantes = tempoPartida % 60;
double tempoMedioPorQuestao = tempoPartida * 1.0 / totalQuestoes;


String classificacao = "Iniciante";
if (percentualGeral >= 90.0) {
    classificacao = "Mestre das Marcas";
} else if (percentualGeral >= 70.0) {
    classificacao = "Conhecedor de Marcas";
} else if (percentualGeral >= 50.0) {
    classificacao = "Aprendiz";
}


String ritmo = "Pausado";
if (tempoMedioPorQuestao <= 10.0) {
    ritmo = "Rápido";
} else if (tempoMedioPorQuestao <= 20.0) {
    ritmo = "Normal";
}
String melhorNivel = "Nenhum";

if (questoesDificeis > 0 && (questoesMedias == 0 || pctDificil >= pctMedio) &&  (questoesFaceis == 0 || pctDificil >= pctFaacil)) {
    melhorNivel = "Difícil";
} else if (questoesMedias > 0 && 
          (questoesFaceis == 0 || pctMedio >= pctFaacil)) {
    melhorNivel = "Médio";
} else if (questoesFaceis > 0) {
    melhorNivel = "Fácil";
}



Console.WriteLine("===== ARCOR – DESAFIO DAS MARCAS: RESUMO DA PARTIDA =====");
Console.WriteLine("Jogador: " + nickname);
Console.WriteLine("Faixa etária: " + nomeFaixa);
Console.WriteLine();
Console.WriteLine("Desempenho por nível:");


if (questoesFaceis > 0) {
    Console.Write("Fácil   (" + acertosFaceis + "/" + questoesFaceis + ")   " + pctFaacil.ToString("F1") + "%   ");
    for (int i = 0; i < acertosFaceis; i++) {
        Console.Write("*");
    }
    Console.WriteLine();
} else {
    Console.WriteLine("Fácil   não jogado");
}


if (questoesMedias > 0) {
    Console.Write("Médio   (" + acertosmMedias + "/" + questoesMedias + ")   " + pctMedio.ToString("F1") + "%   ");
    for (int i = 0; i < acertosmMedias; i++) {
        Console.Write("*");
    }
    Console.WriteLine();
} else {
    Console.WriteLine("Médio   não jogado");
}

if (questoesDificeis > 0) {
    Console.Write("Difícil (" + acertosDificeis + "/" + questoesDificeis + ")   " + pctDificil.ToString("F1") + "%   ");
    for (int i = 0; i < acertosDificeis; i++) {
        Console.Write("*");
    }
    Console.WriteLine();
} else {
    Console.WriteLine("Difícil não jogado");
}

Console.WriteLine();
Console.WriteLine("Total: " + totalAcertos + " acertos e " + totalErros + " erros em " + totalQuestoes + " questões (" + percentualGeral.ToString("F1") + "%)");
Console.WriteLine("Pontuação: " + pontuacao + " de " + pontuacaoMaxima + " pontos possíveis (" + aproveitamentoPontuacao.ToString("F1") + "%)");
Console.WriteLine("Dicas usadas: " + dicasUsadas + " (penalidade de " + penalidadeDicas + " pontos)");
Console.WriteLine("Tempo total: " + minutos + " min " + segundosRestantes + " s | Média: " + tempoMedioPorQuestao.ToString("F1") + " s por questão");
Console.WriteLine("Ritmo: " + ritmo);
Console.WriteLine("Melhor nível: " + melhorNivel);
Console.WriteLine("Classificação: " + classificacao);
Console.WriteLine("=================================================================");