SELECT
    RelicId,
    SlotNumber,
    EffectId
FROM RelicEffect
WHERE EffectId = $effectId
ORDER BY RelicId ASC, SlotNumber ASC;
