# Etiqueta de locação

O modelo padrão é preparado para etiquetas de **100 mm × 50 mm**.

- O QR Code mede 32 mm e fica centralizado horizontalmente.
- A locação visível ocupa 97 mm de largura útil e mantém os pontos separadores.
- O tamanho da fonte é calculado no navegador para códigos com 9, 10, 11 ou 12 caracteres, sem contar os pontos.
- O conteúdo codificado no QR Code é convertido para maiúsculas e não contém pontos nem espaços.
- Na impressão, o bloco de 100 mm é centralizado na largura física informada pelo driver.
- O HTML não define o tamanho da mídia em `@page`; assim, o navegador preserva o tamanho configurado no driver da impressora em vez de substituí-lo para o trabalho.

Para preservar as medidas, mantenha o papel configurado no driver, use margens **nenhuma** e escala **100%** no diálogo de impressão.
