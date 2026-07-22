SELECT
    RelicId,
    SlotNumber,
    EffectId
FROM RelicEffect
WHERE RelicId = $relicId
  AND SlotNumber = $slotNumber;
