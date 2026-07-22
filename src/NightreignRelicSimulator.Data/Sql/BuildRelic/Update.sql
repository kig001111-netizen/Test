UPDATE BuildRelic
SET RelicId = $relicId
WHERE BuildId = $buildId
  AND Position = $position;
