SELECT
    Id,
    EffectId,
    Name,
    Category,
    CanStack,
    Value,
    Level,
    Description,
    DisplayOrder
FROM Effect
WHERE Id = $id;
