SELECT
    RelicId,
    SlotNumber,
    EffectId
FROM RelicEffect
WHERE RelicId = $relicId
ORDER BY SlotNumber ASC;
