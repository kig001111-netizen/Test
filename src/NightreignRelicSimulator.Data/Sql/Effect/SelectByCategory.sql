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
WHERE Category = $category
ORDER BY DisplayOrder ASC, EffectId ASC, Level ASC, Id ASC;
