/** ブラウザ内 SQLite を使う Api（旧サーバー API と同じインターフェース） */
const Api = (() => {
  function allEffects() {
    return Db.exec(
      "SELECT Id, EffectId, Name, Category, CanStack, Value, Level, Description, DisplayOrder FROM Effect ORDER BY DisplayOrder, EffectId, Level"
    ).map(Db.mapEffect);
  }

  function getEffect(id) {
    const rows = Db.exec(
      "SELECT Id, EffectId, Name, Category, CanStack, Value, Level, Description, DisplayOrder FROM Effect WHERE Id = ?",
      [id]
    ).map(Db.mapEffect);
    return rows[0] || null;
  }

  async function getEffects(params = {}) {
    let list = allEffects();
    if (params.q) {
      const q = String(params.q).toLowerCase();
      list = list.filter((e) => e.name.toLowerCase().includes(q));
    } else if (params.category) {
      list = list.filter((e) => e.category === params.category);
    }
    if (params.forRelic === true || params.forRelic === "true") {
      list = Calc.collapseForRelicSelection(list);
    }
    return list;
  }

  async function getStagedEffects() {
    return Calc.getStagedDefinitions(allEffects());
  }

  async function createEffect(body) {
    Db.run(
      `INSERT INTO Effect (EffectId, Name, Category, CanStack, Value, Level, Description, DisplayOrder)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        body.effectId,
        body.name,
        body.category || "",
        body.canStack ? 1 : 0,
        body.value,
        body.level || 1,
        body.description || "",
        body.displayOrder || 0
      ]
    );
    await Db.persist();
    const id = Db.exec("SELECT last_insert_rowid() AS Id")[0].Id;
    return getEffect(id);
  }

  async function updateEffect(id, body) {
    Db.run(
      `UPDATE Effect SET EffectId=?, Name=?, Category=?, CanStack=?, Value=?, Level=?, Description=?, DisplayOrder=?
       WHERE Id=?`,
      [
        body.effectId,
        body.name,
        body.category || "",
        body.canStack ? 1 : 0,
        body.value,
        body.level || 1,
        body.description || "",
        body.displayOrder || 0,
        id
      ]
    );
    await Db.persist();
  }

  async function deleteEffect(id) {
    const used = Db.exec("SELECT 1 AS X FROM RelicEffect WHERE EffectId = ? LIMIT 1", [id]);
    if (used.length) {
      throw new Error("この効果は遺物で使用中のため削除できません。");
    }
    Db.run("DELETE FROM Effect WHERE Id = ?", [id]);
    await Db.persist();
  }

  async function getRelics(params = {}) {
    let sql = "SELECT Id, Name, Color, Memo, CreatedAt, UpdatedAt FROM Relic";
    const args = [];
    if (params.q) {
      sql += " WHERE Name LIKE ?";
      args.push(`%${params.q}%`);
    } else if (params.color !== undefined && params.color !== "") {
      sql += " WHERE Color = ?";
      args.push(Number(params.color));
    }
    sql += " ORDER BY Id";
    return Db.exec(sql, args).map(Db.mapRelic);
  }

  async function getRelic(id) {
    const relicRows = Db.exec(
      "SELECT Id, Name, Color, Memo, CreatedAt, UpdatedAt FROM Relic WHERE Id = ?",
      [id]
    ).map(Db.mapRelic);
    if (!relicRows.length) return null;
    const slots = Db.exec(
      `SELECT re.SlotNumber, e.Id, e.EffectId, e.Name, e.Category, e.CanStack, e.Value, e.Level, e.Description, e.DisplayOrder
       FROM RelicEffect re
       INNER JOIN Effect e ON e.Id = re.EffectId
       WHERE re.RelicId = ?
       ORDER BY re.SlotNumber`,
      [id]
    ).map((row) => ({
      slotNumber: row.SlotNumber,
      effect: Db.mapEffect({
        Id: row.Id,
        EffectId: row.EffectId,
        Name: row.Name,
        Category: row.Category,
        CanStack: row.CanStack,
        Value: row.Value,
        Level: row.Level,
        Description: row.Description,
        DisplayOrder: row.DisplayOrder
      })
    }));
    return { relic: relicRows[0], slots };
  }

  function replaceRelicEffects(relicId, effectIdsBySlot) {
    Db.run("DELETE FROM RelicEffect WHERE RelicId = ?", [relicId]);
    (effectIdsBySlot || []).forEach((effectId, index) => {
      if (effectId == null) return;
      Db.run(
        "INSERT INTO RelicEffect (RelicId, SlotNumber, EffectId) VALUES (?, ?, ?)",
        [relicId, index + 1, effectId]
      );
    });
  }

  async function createRelic(body) {
    const ts = Db.nowIso();
    Db.run(
      "INSERT INTO Relic (Name, Color, Memo, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?)",
      [body.name, body.color || 0, body.memo || "", ts, ts]
    );
    const id = Db.exec("SELECT last_insert_rowid() AS Id")[0].Id;
    replaceRelicEffects(id, body.effectIdsBySlot);
    await Db.persist();
    return getRelic(id);
  }

  async function updateRelic(id, body) {
    Db.run(
      "UPDATE Relic SET Name=?, Color=?, Memo=?, UpdatedAt=? WHERE Id=?",
      [body.name, body.color || 0, body.memo || "", Db.nowIso(), id]
    );
    replaceRelicEffects(id, body.effectIdsBySlot);
    await Db.persist();
  }

  async function deleteRelic(id) {
    const used = Db.exec("SELECT 1 AS X FROM BuildRelic WHERE RelicId = ? LIMIT 1", [id]);
    if (used.length) {
      throw new Error("この遺物はビルドで使用中のため削除できません。");
    }
    Db.run("DELETE FROM RelicEffect WHERE RelicId = ?", [id]);
    Db.run("DELETE FROM Relic WHERE Id = ?", [id]);
    await Db.persist();
  }

  async function getBuilds(params = {}) {
    let sql = "SELECT Id, Name, CharacterName, WeaponName, CreatedAt, UpdatedAt FROM Build";
    const args = [];
    if (params.q) {
      sql += " WHERE Name LIKE ?";
      args.push(`%${params.q}%`);
    }
    sql += " ORDER BY Id";
    return Db.exec(sql, args).map(Db.mapBuild);
  }

  async function getBuild(id) {
    const builds = Db.exec(
      "SELECT Id, Name, CharacterName, WeaponName, CreatedAt, UpdatedAt FROM Build WHERE Id = ?",
      [id]
    ).map(Db.mapBuild);
    if (!builds.length) return null;
    const slots = Db.exec(
      `SELECT br.Position, r.Id, r.Name, r.Color, r.Memo, r.CreatedAt, r.UpdatedAt
       FROM BuildRelic br
       INNER JOIN Relic r ON r.Id = br.RelicId
       WHERE br.BuildId = ?
       ORDER BY br.Position`,
      [id]
    ).map((row) => ({
      position: row.Position,
      relic: Db.mapRelic({
        Id: row.Id,
        Name: row.Name,
        Color: row.Color,
        Memo: row.Memo,
        CreatedAt: row.CreatedAt,
        UpdatedAt: row.UpdatedAt
      })
    }));
    return { build: builds[0], slots };
  }

  function replaceBuildRelics(buildId, relicIdsByPosition) {
    Db.run("DELETE FROM BuildRelic WHERE BuildId = ?", [buildId]);
    (relicIdsByPosition || []).forEach((relicId, index) => {
      if (relicId == null) return;
      Db.run(
        "INSERT INTO BuildRelic (BuildId, Position, RelicId) VALUES (?, ?, ?)",
        [buildId, index + 1, relicId]
      );
    });
  }

  async function saveBuild(body) {
    const ts = Db.nowIso();
    let id = body.id;
    if (id) {
      Db.run(
        "UPDATE Build SET Name=?, CharacterName=?, WeaponName=?, UpdatedAt=? WHERE Id=?",
        [body.name, body.characterName || "", body.weaponName || "", ts, id]
      );
    } else {
      Db.run(
        "INSERT INTO Build (Name, CharacterName, WeaponName, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?)",
        [body.name, body.characterName || "", body.weaponName || "", ts, ts]
      );
      id = Db.exec("SELECT last_insert_rowid() AS Id")[0].Id;
    }
    replaceBuildRelics(id, body.relicIdsByPosition);
    await Db.persist();
    return getBuild(id);
  }

  async function deleteBuild(id) {
    Db.run("DELETE FROM BuildRelic WHERE BuildId = ?", [id]);
    Db.run("DELETE FROM Build WHERE Id = ?", [id]);
    await Db.persist();
  }

  async function calculate(body) {
    const detail = await getBuild(body.buildId);
    if (!detail) {
      throw new Error("ビルドが見つかりません。");
    }

    const effects = [];
    for (const slot of detail.slots.slice().sort((a, b) => a.position - b.position)) {
      const relic = await getRelic(slot.relic.id);
      if (!relic) continue;
      relic.slots
        .slice()
        .sort((a, b) => a.slotNumber - b.slotNumber)
        .forEach((s) => effects.push(s.effect));
    }

    const catalog = allEffects();
    const result = Calc.calculate({
      weaponAttack: Number(body.weaponAttack) || 0,
      effects,
      catalog,
      levelOverrides: body.levelOverrides || {}
    });

    const stagedControls = Calc.getStagedDefinitions(catalog)
      .filter((d) => effects.some((e) => e.effectId === d.effectId))
      .map((d) => {
        const selected = body.levelOverrides && body.levelOverrides[d.effectId] != null
          ? Number(body.levelOverrides[d.effectId])
          : effects.find((e) => e.effectId === d.effectId).level;
        return {
          effectId: d.effectId,
          name: d.name,
          selectedLevel: selected,
          levels: d.levels
        };
      });

    return { ...result, stagedControls };
  }

  async function loadBuildMatrix(buildId) {
    const detail = await getBuild(buildId);
    if (!detail) return null;
    const catalog = allEffects();
    const collapsed = Calc.collapseForRelicSelection(catalog);
    const byEffectId = new Map(collapsed.map((e) => [e.effectId, e.id]));

    const effectIdsByRelic = [[], [], [], [], [], []];
    for (let position = 1; position <= 6; position++) {
      const slot = detail.slots.find((s) => s.position === position);
      if (!slot) continue;
      const relic = await getRelic(slot.relic.id);
      if (!relic) continue;
      effectIdsByRelic[position - 1] = relic.slots
        .slice()
        .sort((a, b) => a.slotNumber - b.slotNumber)
        .map((s) => byEffectId.get(s.effect.effectId) ?? s.effect.id)
        .slice(0, 3);
    }

    return { build: detail.build, effectIdsByRelic };
  }

  async function saveBuildMatrix(body) {
    const name = (body.name || "").trim();
    if (!name) throw new Error("ビルド名は必須です。");
    const columns = body.effectIdsByRelic || [];
    if (columns.length !== 6) throw new Error("遺物列は6件で指定してください。");

    let previousByPos = {};
    let previousIds = [];
    if (body.id) {
      const prev = await getBuild(body.id);
      if (!prev) throw new Error("ビルドが見つかりません。");
      prev.slots.forEach((s) => {
        previousByPos[s.position] = s.relic.id;
        previousIds.push(s.relic.id);
      });
    }

    const relicIdsByPosition = [null, null, null, null, null, null];
    const used = new Set();

    for (let i = 0; i < 6; i++) {
      const ids = (columns[i] || []).filter((x) => x != null).slice(0, 3);
      if (!ids.length) continue;
      const slots = [ids[0] ?? null, ids[1] ?? null, ids[2] ?? null];
      const position = i + 1;
      const payload = {
        name: `${name} #${position}`,
        color: 0,
        memo: "matrix",
        effectIdsBySlot: slots
      };
      if (previousByPos[position]) {
        await updateRelic(previousByPos[position], { ...payload, id: previousByPos[position] });
        relicIdsByPosition[i] = previousByPos[position];
        used.add(previousByPos[position]);
      } else {
        const created = await createRelic(payload);
        relicIdsByPosition[i] = created.relic.id;
        used.add(created.relic.id);
      }
    }

    const saved = await saveBuild({
      id: body.id || null,
      name,
      characterName: body.characterName || "",
      weaponName: body.weaponName || "",
      relicIdsByPosition
    });

    for (const oldId of previousIds) {
      if (used.has(oldId)) continue;
      try {
        await deleteRelic(oldId);
      } catch {
        /* ignore */
      }
    }

    return loadBuildMatrix(saved.build.id);
  }

  async function calculateMatrix(body) {
    const catalog = allEffects();
    const byId = new Map(catalog.map((e) => [e.id, e]));
    const effects = [];
    (body.effectIdsByRelic || []).forEach((column) => {
      (column || []).forEach((id) => {
        const effect = byId.get(Number(id));
        if (effect) effects.push(effect);
      });
    });

    const result = Calc.calculate({
      weaponAttack: Number(body.weaponAttack) || 0,
      effects,
      catalog,
      levelOverrides: body.levelOverrides || {}
    });

    const stagedControls = Calc.getStagedDefinitions(catalog)
      .filter((d) => effects.some((e) => e.effectId === d.effectId))
      .map((d) => {
        const selected = body.levelOverrides && body.levelOverrides[d.effectId] != null
          ? Number(body.levelOverrides[d.effectId])
          : effects.find((e) => e.effectId === d.effectId).level;
        return {
          effectId: d.effectId,
          name: d.name,
          selectedLevel: selected,
          levels: d.levels
        };
      });

    return { ...result, stagedControls };
  }

  return {
    getEffects,
    getStagedEffects,
    createEffect,
    updateEffect,
    deleteEffect,
    getRelics,
    getRelic,
    createRelic,
    updateRelic,
    deleteRelic,
    getBuilds,
    getBuild,
    saveBuild,
    deleteBuild,
    calculate,
    loadBuildMatrix,
    saveBuildMatrix,
    calculateMatrix
  };
})();
