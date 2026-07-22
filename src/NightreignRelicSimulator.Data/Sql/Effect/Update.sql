UPDATE Effect
SET
    EffectId = $effectId,
    Name = $name,
    Category = $category,
    CanStack = $canStack,
    Value = $value,
    Level = $level,
    Description = $description,
    DisplayOrder = $displayOrder
WHERE Id = $id;
