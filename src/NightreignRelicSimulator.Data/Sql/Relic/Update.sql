UPDATE Relic
SET
    Name = $name,
    Color = $color,
    Memo = $memo,
    UpdatedAt = $updatedAt
WHERE Id = $id;
