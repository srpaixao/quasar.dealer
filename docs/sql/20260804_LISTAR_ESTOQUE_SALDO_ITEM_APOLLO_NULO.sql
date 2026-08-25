/*
    Lista os registros de estoque com saldo positivo cujo material ainda
    nao possui o Item Apollo preenchido.
*/

SELECT
    E.Id,
    E.ItemNr,
    M.Descricao AS MaterialDescricao,
    M.ItemApollo,
    E.Locacao,
    E.Saldo,
    E.Indisponivel,
    E.PedidoPendente,
    E.FilialId
FROM Estoque E
INNER JOIN Material M
    ON M.Codigo COLLATE DATABASE_DEFAULT = E.ItemNr COLLATE DATABASE_DEFAULT
WHERE ISNULL(E.Saldo, 0) > 0
  AND NULLIF(LTRIM(RTRIM(M.ItemApollo)), '') IS NULL
ORDER BY
    E.ItemNr,
    E.Locacao;
