# Atualizações Operacionais - 25/08/2026

## Escopo

Esta entrega consolida melhorias no coletor, na API operacional e na documentação do Quasar Dealer para recebimento, estoque e expedição.

## Recebimento - Descarregar

- O card `Pendentes` passou a ser interativo.
- Ao tocar no card, o coletor consulta novamente a área selecionada e mostra somente os números dos volumes ainda pendentes.
- A lista possui atualização manual, rolagem e restaura o foco do leitor ao ser fechada.
- O indicador anteriormente apresentado como `Confirmados` foi padronizado para `Conferidos`.
- O campo `Volume NR` mantém o teclado virtual desativado por padrão para preservar a visualização dos cards.
- O ícone de teclado habilita ou desabilita a digitação manual.
- A leitura é processada quando o leitor envia `Enter` ou `Tab` como terminador.

## Recebimento - Conferir

- A conferência quantitativa por volume está operacional no coletor.
- Cada item apresenta quantidade faturada, conferida, armazenada, diferença e situação.
- A confirmação registra operador e data/hora.
- Divergências a maior ou a menor exigem confirmação explícita.
- Itens críticos apresentam alerta e observação cadastrada para o material.
- O controle de concorrência impede sobrescrita silenciosa quando outro operador altera o item.

## Estoque - Coletar

- A operação inicia pela leitura e validação da `Locação de Espera`.
- O item é consultado somente após a validação da localização.
- A tela exibe origem, saldo e quantidade a coletar.
- A coleta cria ou atualiza a movimentação pendente destinada à Locação de Espera.
- Quantidade inválida ou superior ao saldo disponível é bloqueada.

## Estoque - Transferir

- A transferência inicia pela consulta da `Locação de Espera`.
- A tela lista todas as movimentações pendentes dessa localização.
- O operador seleciona o item, confirma a Locação Final e informa a quantidade.
- Quando a movimentação está íntegra, a quantidade transferida deve corresponder à quantidade coletada.
- Após a confirmação, a lista da Locação de Espera é recarregada.

## Expedição

- O coletor possui fluxo de `Conferir Separação` por romaneio.
- O romaneio é assumido pelo operador, protegido contra concorrência e pode ser liberado ao abandonar a operação.
- Quantidade zero envia o item para `Em Busca` após confirmação.
- A conferência de volumes de expedição mantém listas detalhadas de pendentes e conferidos.

## Versão e Publicação

- O coletor exibe a versão da aplicação no login e na barra superior.
- A versão do coletor desta entrega é `1.0.7`.
- O build de produção usa `.env.production` e gera a aplicação em `dist/`.
- A aplicação do coletor é publicada separadamente da API e do Web MVC.

## Validações Recomendadas

1. Selecionar uma área de recebimento e abrir o card `Pendentes`.
2. Conferir se a lista apresenta somente `VolumeNr` com `StatusId = 1`.
3. Ler um volume com o leitor configurado para terminar em `Enter` ou `Tab`.
4. Confirmar que o teclado virtual não aparece automaticamente.
5. Acionar o ícone de teclado e validar a digitação manual.
6. Validar coleta e transferência usando uma Locação de Espera com movimentações conhecidas.
7. Executar conferência de recebimento com quantidade correta e com divergência.

