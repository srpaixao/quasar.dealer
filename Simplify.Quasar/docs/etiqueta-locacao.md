# Etiqueta de locação

O modelo padrão é preparado para etiquetas de **100 mm × 50 mm**.

- O QR Code mede 32 mm e fica centralizado horizontalmente.
- A locação visível ocupa 97 mm de largura útil e mantém os pontos separadores.
- O tamanho da fonte é calculado no navegador para códigos com 9, 10, 11 ou 12 caracteres, sem contar os pontos.
- O conteúdo codificado no QR Code é convertido para maiúsculas e não contém pontos nem espaços.
- Na impressão, o bloco de 100 mm é centralizado na largura física informada pelo driver.
- O HTML não envia tamanho de mídia em `@page`; a largura do trabalho fica exclusivamente sob controle do perfil de 832 pontos configurado no driver da Zebra.

Para preservar as medidas, mantenha o papel configurado no driver, use margens **nenhuma** e escala **100%** no diálogo de impressão.

O único fluxo disponível é a impressão pelo preview do navegador. QR Code, texto, dimensões e posições são exatamente os mesmos no preview e na impressão; ao imprimir, apenas a barra de ações e a borda visual são ocultadas.
