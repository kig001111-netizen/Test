/** 火力計算・段階効果・重複判定（C# DamageCalculator 相当） */
const Calc = (() => {
  function getStagedDefinitions(catalog) {
    const groups = new Map();
    catalog.forEach((e) => {
      if (!groups.has(e.effectId)) groups.set(e.effectId, []);
      groups.get(e.effectId).push(e);
    });

    return [...groups.entries()]
      .filter(([, list]) => new Set(list.map((x) => x.level)).size > 1)
      .map(([effectId, list]) => {
        const ordered = list.slice().sort((a, b) => a.level - b.level);
        return {
          effectId,
          name: ordered[0].name,
          category: ordered[0].category,
          canStack: ordered[0].canStack,
          levels: ordered.map((x) => ({
            rowId: x.id,
            level: x.level,
            value: x.value
          }))
        };
      });
  }

  function collapseForRelicSelection(catalog) {
    const best = new Map();
    catalog.forEach((e) => {
      const prev = best.get(e.effectId);
      if (!prev || e.level < prev.level || (e.level === prev.level && e.id < prev.id)) {
        best.set(e.effectId, e);
      }
    });
    return [...best.values()].sort((a, b) => a.displayOrder - b.displayOrder || a.effectId - b.effectId);
  }

  function applyLevelOverrides(equipped, catalog, levelOverrides) {
    const byId = new Map();
    catalog.forEach((e) => {
      if (!byId.has(e.effectId)) byId.set(e.effectId, []);
      byId.get(e.effectId).push(e);
    });
    byId.forEach((list) => list.sort((a, b) => a.level - b.level));

    return equipped.map((effect) => {
      const levels = byId.get(effect.effectId) || [];
      if (levels.length <= 1) return effect;
      const target = levelOverrides && levelOverrides[effect.effectId] != null
        ? Number(levelOverrides[effect.effectId])
        : effect.level;
      return levels.find((l) => l.level === target) || levels[0];
    });
  }

  function duplicateCheck(effects) {
    const applied = [];
    const ignored = [];
    const seen = new Set();
    effects.forEach((effect) => {
      if (effect.canStack) {
        applied.push(effect);
        return;
      }
      if (seen.has(effect.effectId)) {
        ignored.push(effect);
      } else {
        seen.add(effect.effectId);
        applied.push(effect);
      }
    });
    return { applied, ignored };
  }

  function calculate({ weaponAttack, effects, catalog, levelOverrides }) {
    const resolvedInput = applyLevelOverrides(effects, catalog, levelOverrides || {});
    const logs = [];
    let step = 0;
    logs.push({ step: ++step, description: `効果入力件数=${resolvedInput.length}`, multiplier: null, currentAttack: weaponAttack });

    const dup = duplicateCheck(resolvedInput);
    logs.push({
      step: ++step,
      description: `重複判定: 採用=${dup.applied.length}, 無効=${dup.ignored.length}`,
      multiplier: null,
      currentAttack: weaponAttack
    });

    logs.push({
      step: ++step,
      description: `条件判定後件数=${dup.applied.length}`,
      multiplier: null,
      currentAttack: weaponAttack
    });

    let totalMultiplier = 1;
    const applied = [];
    dup.applied.forEach((effect) => {
      totalMultiplier *= effect.value;
      const currentAttack = weaponAttack * totalMultiplier;
      applied.push(effect);
      logs.push({
        step: ++step,
        description: `適用: EffectId=${effect.effectId} Lv${effect.level} ${effect.name}`,
        multiplier: effect.value,
        currentAttack
      });
    });

    const finalAttack = weaponAttack * totalMultiplier;
    logs.push({ step: ++step, description: "最終火力算出", multiplier: totalMultiplier, currentAttack: finalAttack });

    return {
      baseAttack: weaponAttack,
      totalMultiplier,
      finalAttack,
      appliedEffects: applied,
      ignoredEffects: dup.ignored,
      logs
    };
  }

  return {
    getStagedDefinitions,
    collapseForRelicSelection,
    calculate
  };
})();
