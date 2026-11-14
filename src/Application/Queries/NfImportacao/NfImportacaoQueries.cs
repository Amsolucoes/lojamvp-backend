namespace LojaApi.Application.NfImportacao;

// Consultas de NfImportacao: só leem dados (cada uma tem o seu handler em Application/Handlers/NfImportacao).

// Arquivo = o XML enviado (nulo quando veio vazio). O handler só lê; quem abriu o stream fecha.
public record PreviewNfImportacaoQuery(Guid? LojaId, Stream? Arquivo);
public record DetalheNfManualQuery(Guid? LojaId, Guid Id);
public record HistoricoNfImportacaoQuery(Guid? LojaId);
