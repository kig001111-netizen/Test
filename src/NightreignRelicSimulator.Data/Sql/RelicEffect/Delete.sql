DELETE FROM RelicEffect
WHERE RelicId = $relicId
  AND SlotNumber = $slotNumber;
