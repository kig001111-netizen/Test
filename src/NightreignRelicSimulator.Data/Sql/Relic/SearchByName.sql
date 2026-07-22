SELECT
    Id,
    Name,
    Color,
    Memo,
    CreatedAt,
    UpdatedAt
FROM Relic
WHERE Name LIKE '%' || $keyword || '%'
ORDER BY Id ASC;
