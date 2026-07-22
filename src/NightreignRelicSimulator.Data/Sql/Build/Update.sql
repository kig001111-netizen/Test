UPDATE Build
SET
    Name = $name,
    CharacterName = $characterName,
    WeaponName = $weaponName,
    UpdatedAt = $updatedAt
WHERE Id = $id;
