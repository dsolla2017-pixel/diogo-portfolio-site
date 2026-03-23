// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: TarefaAutomatizada.cs
// Descrição: Modelo que representa uma tarefa automatizada pelo agente de IA
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

namespace CreditoPrice.AiAgent.Models
{
    /// <summary>
    /// Representa uma tarefa automatizada pelo Agente de IA,
    /// com métricas de produtividade e economia.
    /// </summary>
    public class TarefaAutomatizada
    {
        /// <summary>Identificador único da execução da tarefa.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Nome da tarefa automatizada (ex.: "ValidacaoContrato").</summary>
        public string NomeTarefa { get; set; } = string.Empty;

        /// <summary>Categoria da tarefa (Validação, Classificação, Relatório, Exceção).</summary>
        public CategoriaTarefa Categoria { get; set; }

        /// <summary>Status da execução (Pendente, EmExecucao, Concluida, Erro).</summary>
        public StatusTarefa Status { get; set; } = StatusTarefa.Pendente;

        /// <summary>Timestamp de início da execução.</summary>
        public DateTime InicioExecucao { get; set; }

        /// <summary>Timestamp de conclusão da execução.</summary>
        public DateTime? FimExecucao { get; set; }

        /// <summary>Tempo de execução em milissegundos.</summary>
        public long TempoExecucaoMs { get; set; }

        /// <summary>
        /// Tempo estimado para execução manual da mesma tarefa (em minutos).
        /// Base: pesquisa interna com analistas da CESOA.
        /// </summary>
        public decimal TempoManualEstimadoMin { get; set; }

        /// <summary>
        /// Custo estimado da execução manual (R$).
        /// Base: custo hora/analista = R$ 75,00 (salário + encargos).
        /// </summary>
        public decimal CustoManualEstimadoBrl { get; set; }

        /// <summary>
        /// Custo real da execução automatizada (R$).
        /// Base: custo computacional Azure (CPU + memória + rede).
        /// </summary>
        public decimal CustoAutomatizadoBrl { get; set; }

        /// <summary>Economia gerada pela automação (R$).</summary>
        public decimal EconomiaBrl => CustoManualEstimadoBrl - CustoAutomatizadoBrl;

        /// <summary>Percentual de economia em relação ao custo manual.</summary>
        public decimal EconomiaPct => CustoManualEstimadoBrl > 0
            ? Math.Round((EconomiaBrl / CustoManualEstimadoBrl) * 100, 1)
            : 0;

        /// <summary>Quantidade de itens processados na tarefa.</summary>
        public int ItensProcessados { get; set; }

        /// <summary>Quantidade de anomalias detectadas pelo agente.</summary>
        public int AnomaliasDetectadas { get; set; }

        /// <summary>Confiança do agente na execução (0 a 100%).</summary>
        public decimal ConfiancaPct { get; set; }

        /// <summary>Identificador de correlação para rastreabilidade.</summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>Detalhes da execução (log resumido).</summary>
        public string Detalhes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Categorias de tarefas automatizáveis pelo Agente de IA.
    /// </summary>
    public enum CategoriaTarefa
    {
        /// <summary>Validação de dados de entrada (campos, tipos, ranges).</summary>
        ValidacaoDados = 1,

        /// <summary>Classificação de risco do contrato (score de crédito).</summary>
        ClassificacaoRisco = 2,

        /// <summary>Conferência automática de cálculos financeiros.</summary>
        ConferenciaCalculo = 3,

        /// <summary>Geração automática de relatórios gerenciais.</summary>
        GeracaoRelatorio = 4,

        /// <summary>Tratamento inteligente de exceções e anomalias.</summary>
        TratamentoExcecao = 5,

        /// <summary>Detecção de padrões e anomalias em lote.</summary>
        DeteccaoAnomalias = 6,

        /// <summary>Enriquecimento de dados com fontes externas.</summary>
        EnriquecimentoDados = 7
    }

    /// <summary>
    /// Status de execução da tarefa automatizada.
    /// </summary>
    public enum StatusTarefa
    {
        Pendente = 0,
        EmExecucao = 1,
        Concluida = 2,
        Erro = 3,
        Cancelada = 4
    }
}
