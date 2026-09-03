# Etiqueta de locação

O modelo padrão é preparado para etiquetas de **100 mm × 50 mm**.

- O QR Code mede 32 mm e fica centralizado horizontalmente.
- A locação visível ocupa 97 mm de largura útil e mantém os pontos separadores.
- O tamanho da fonte é calculado no navegador para códigos com 9, 10, 11 ou 12 caracteres, sem contar os pontos.
- O conteúdo codificado no QR Code é convertido para maiúsculas e não contém pontos nem espaços.
- Na impressão, o bloco de 100 mm é centralizado na largura física informada pelo driver.
- O HTML não define o tamanho da mídia em `@page`; assim, o navegador preserva o tamanho configurado no driver da impressora em vez de substituí-lo para o trabalho.

Para preservar as medidas, mantenha o papel configurado no driver, use margens **nenhuma** e escala **100%** no diálogo de impressão.

## Impressão direta na Zebra ZT230

Quando as impressoras estão cadastradas, o preview oferece um DDL com as impressoras válidas da filial e o botão **Imprimir direto**. O cadastro `Recebimento` é sempre pré-selecionado quando disponível; as demais impressoras continuam acessíveis no DDL. Essa opção faz o servidor do Quasar enviar ZPL nativo diretamente à porta cadastrada, com 832 × 400 pontos para a ZT230 de 203 dpi. Ela não depende do navegador, do driver nem do certificado do antigo servidor intermediário. O botão convencional permanece disponível.
