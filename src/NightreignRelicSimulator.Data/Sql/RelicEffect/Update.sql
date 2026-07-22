UPDATE RelicEffect
SET EffectId = $effectId
WHERE RelicId = $relicId
  AND SlotNumber = $slotNumber;
