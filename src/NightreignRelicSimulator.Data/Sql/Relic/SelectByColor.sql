SELECT
    Id,
    Name,
    Color,
    Memo,
    CreatedAt,
    UpdatedAt
FROM Relic
WHERE Color = $color
ORDER BY Id ASC;
