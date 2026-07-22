SELECT
    Id,
    Name,
    Color,
    Memo,
    CreatedAt,
    UpdatedAt
FROM Relic
WHERE Id = $id;
