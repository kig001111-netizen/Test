DELETE FROM BuildRelic
WHERE BuildId = $buildId
  AND Position = $position;
