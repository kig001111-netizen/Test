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
WHERE Name LIKE '%' || $keyword || '%'
ORDER BY DisplayOrder ASC, EffectId ASC, Level ASC, Id ASC;
