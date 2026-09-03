# Etiqueta de locação

O modelo padrão é preparado para etiquetas de **100 mm × 50 mm**.

- O QR Code mede 32 mm e fica centralizado horizontalmente.
- A locação visível ocupa 97 mm de largura útil e mantém os pontos separadores.
- O tamanho da fonte é calculado no navegador para códigos com 9, 10, 11 ou 12 caracteres, sem contar os pontos.
- O conteúdo codificado no QR Code é convertido para maiúsculas e não contém pontos nem espaços.
- Na impressão, o bloco de 100 mm é centralizado na largura física informada pelo driver.
- A página de impressão usa 104 × 50 mm: 104 mm correspondem aos 832 pontos da Zebra ZT230 em 203 dpi, mantendo o conteúdo útil centralizado em 100 × 50 mm.

Para preservar as medidas, mantenha o papel configurado no driver, use margens **nenhuma** e escala **100%** no diálogo de impressão.

O único fluxo disponível é a impressão pelo preview do navegador. O texto é recalculado ao entrar no modo de impressão para aproveitar a largura física correta.

Como compensação do resultado físico informado, QR Code e texto são deslocados 15 mm à direita somente na impressão. A fonte recebe 10 pontos adicionais e uma área horizontal 20 mm maior; o preview permanece inalterado.
