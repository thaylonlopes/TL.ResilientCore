using System.ComponentModel;

namespace TL.ResilientCore.Domain.Enums;

public enum StatusProcessamento
{
    [Description("Processamento Pendente")]
    Pendente = 1,

    [Description("Processamento Concluído com Sucesso")]
    Concluido = 2,

    [Description("Falha na Comunicação com Serviço Externo")]
    FalhaIntegracao = 3
}