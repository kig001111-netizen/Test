const state = {
  weaponAttack: Number(localStorage.getItem("weaponAttack") || 1000),
  selectedBuildId: localStorage.getItem("selectedBuildId")
    ? Number(localStorage.getItem("selectedBuildId"))
    : null,
  effects: [],
  relicEffects: [],
  relics: [],
  builds: [],
  levelOverrides: loadLevelOverrides()
};

function loadLevelOverrides() {
  try {
    return JSON.parse(localStorage.getItem("levelOverrides") || "{}");
  } catch {
    return {};
  }
}

function persistLevelOverrides() {
  localStorage.setItem("levelOverrides", JSON.stringify(state.levelOverrides));
}

const colors = ["None", "Red", "Blue", "Yellow", "Green", "Purple"];
const titles = {
  calc: "火力計算",
  build: "ビルド管理",
  relic: "遺物管理",
  effect: "Effect管理"
};

function $(id) {
  return document.getElementById(id);
}

function saveSession() {
  localStorage.setItem("weaponAttack", String(state.weaponAttack));
  if (state.selectedBuildId == null) {
    localStorage.removeItem("selectedBuildId");
  } else {
    localStorage.setItem("selectedBuildId", String(state.selectedBuildId));
  }

  const buildText = state.selectedBuildId == null ? "Build未選択" : `Build #${state.selectedBuildId}`;
  $("sessionInfo").textContent = `火力 ${state.weaponAttack}\n${buildText}`;
}

function showError(error) {
  alert(error?.message || String(error));
}

function showView(name) {
  document.querySelectorAll(".view").forEach((el) => el.classList.remove("active"));
  document.querySelectorAll(".nav-btn").forEach((el) => el.classList.remove("active"));
  $(`view-${name}`).classList.add("active");
  document.querySelector(`.nav-btn[data-view="${name}"]`)?.classList.add("active");
  $("viewTitle").textContent = titles[name] || name;

  if (name === "calc") {
    loadCalcBuilds();
  } else if (name === "build") {
    loadBuilds();
  } else if (name === "relic") {
    loadRelics();
  } else if (name === "effect") {
    loadEffects();
  }
}

async function loadEffects(params = {}) {
  try {
    state.effects = await Api.getEffects(params);
    const categories = ["(すべて)", ...new Set(state.effects.map((e) => e.category).filter(Boolean))];
    const select = $("effectCategory");
    const current = select.value;
    select.innerHTML = categories.map((c) => `<option value="${c === "(すべて)" ? "" : c}">${c}</option>`).join("");
    if (categories.includes(current) || current === "") {
      select.value = current;
    }

    const category = select.value;
    const rows = category
      ? state.effects.filter((e) => e.category === category)
      : state.effects;

    $("effectTable").innerHTML = rows.map((e) => `
      <tr data-id="${e.id}">
        <td>${e.effectId}</td>
        <td>${escapeHtml(e.name)}</td>
        <td>${escapeHtml(e.category)}</td>
        <td>${e.canStack}</td>
        <td>${e.value}</td>
        <td>${e.level}</td>
        <td>
          <button type="button" class="btn" data-edit="${e.id}">編集</button>
          <button type="button" class="btn danger" data-del="${e.id}">削除</button>
        </td>
      </tr>`).join("");

    fillEffectSlotOptions();
  } catch (error) {
    showError(error);
  }
}

function fillEffectSlotOptions() {
  const source = state.relicEffects.length ? state.relicEffects : state.effects;
  const stagedIds = new Set((state.stagedDefs || []).map((d) => d.effectId));
  const options = [`<option value="">(なし)</option>`]
    .concat(source.map((e) => {
      const label = stagedIds.has(e.effectId)
        ? `${e.effectId}: ${escapeHtml(e.name)}（段階・計算でLv指定）`
        : `${e.effectId}: ${escapeHtml(e.name)}`;
      return `<option value="${e.id}">${label}</option>`;
    }));
  const html = options.join("");
  document.querySelectorAll("#relicEffectSlots select").forEach((el) => {
    const value = el.value;
    el.innerHTML = html;
    el.value = value;
  });
}

function renderStagedControls(controls) {
  const panel = $("stagedPanel");
  const host = $("stagedControls");
  if (!controls || !controls.length) {
    panel.hidden = true;
    host.innerHTML = "";
    return;
  }

  panel.hidden = false;
  host.innerHTML = controls.map((c) => {
    const selected = state.levelOverrides[c.effectId] ?? c.selectedLevel;
    const options = c.levels.map((lv) =>
      `<option value="${lv.level}" ${Number(selected) === lv.level ? "selected" : ""}>Lv${lv.level} (×${lv.value})</option>`
    ).join("");
    return `
      <div class="staged-item">
        <div>
          <strong>${escapeHtml(c.name)}</strong>
          <div class="value-hint">EffectId=${c.effectId} / 同一効果は1件のみ適用</div>
        </div>
        <select data-effect-id="${c.effectId}">${options}</select>
      </div>`;
  }).join("");

  host.querySelectorAll("select[data-effect-id]").forEach((select) => {
    select.addEventListener("change", () => {
      const effectId = Number(select.dataset.effectId);
      state.levelOverrides[effectId] = Number(select.value);
      persistLevelOverrides();
      calculate();
    });
  });
}

function openEffectDialog(effect) {
  $("effectDialogTitle").textContent = effect ? "Effect編集" : "Effect追加";
  $("effRowId").value = effect?.id ?? "";
  $("effEffectId").value = effect?.effectId ?? 0;
  $("effName").value = effect?.name ?? "";
  $("effCategory").value = effect?.category ?? "";
  $("effCanStack").checked = effect?.canStack ?? true;
  $("effValue").value = effect?.value ?? 1.04;
  $("effLevel").value = effect?.level ?? 1;
  $("effDescription").value = effect?.description ?? "";
  $("effDisplayOrder").value = effect?.displayOrder ?? 0;
  $("effectDialog").showModal();
}

async function loadRelics() {
  try {
    const q = $("relicSearch").value.trim();
    const color = $("relicColorFilter").value;
    const params = {};
    if (q) params.q = q;
    else if (color !== "") params.color = color;
    state.relics = await Api.getRelics(params);
    $("relicTable").innerHTML = state.relics.map((r) => `
      <tr data-id="${r.id}">
        <td>${r.id}</td>
        <td>${escapeHtml(r.name)}</td>
        <td>${colors[r.color] ?? r.color}</td>
      </tr>`).join("");
  } catch (error) {
    showError(error);
  }
}

async function loadRelicDetail(id) {
  try {
    const detail = await Api.getRelic(id);
    $("relicEditorTitle").textContent = `編集中 Id=${detail.relic.id}`;
    $("relicId").value = detail.relic.id;
    $("relicName").value = detail.relic.name;
    $("relicColor").value = detail.relic.color;
    $("relicMemo").value = detail.relic.memo ?? "";
    for (let i = 1; i <= 3; i++) {
      const slot = detail.slots.find((s) => s.slotNumber === i);
      if (!slot) {
        $(`relicEffect${i}`).value = "";
        continue;
      }

      const collapsed = state.relicEffects.find((e) => e.effectId === slot.effect.effectId);
      $(`relicEffect${i}`).value = collapsed ? String(collapsed.id) : String(slot.effect.id);
    }
  } catch (error) {
    showError(error);
  }
}

function clearRelicEditor() {
  $("relicEditorTitle").textContent = "新規登録";
  $("relicId").value = "";
  $("relicForm").reset();
  for (let i = 1; i <= 3; i++) {
    $(`relicEffect${i}`).value = "";
  }
}

async function loadBuilds(q = "") {
  try {
    state.builds = await Api.getBuilds(q ? { q } : {});
    $("buildTable").innerHTML = state.builds.map((b) => `
      <tr data-id="${b.id}">
        <td>${b.id}</td>
        <td>${escapeHtml(b.name)}</td>
        <td>${escapeHtml(b.characterName)}</td>
        <td>${escapeHtml(b.weaponName)}</td>
      </tr>`).join("");
    fillBuildRelicOptions();
  } catch (error) {
    showError(error);
  }
}

function fillBuildRelicOptions() {
  const options = [`<option value="">(なし)</option>`]
    .concat(state.relics.map((r) => `<option value="${r.id}">${r.id}: ${escapeHtml(r.name)}</option>`));
  const html = options.join("");
  document.querySelectorAll("#buildRelicSlots select").forEach((el) => {
    const value = el.value;
    el.innerHTML = html;
    el.value = value;
  });
}

async function loadBuildDetail(id) {
  try {
    const detail = await Api.getBuild(id);
    $("buildEditorTitle").textContent = `編集中 Id=${detail.build.id}`;
    $("buildId").value = detail.build.id;
    $("buildName").value = detail.build.name;
    $("buildCharacter").value = detail.build.characterName;
    $("buildWeapon").value = detail.build.weaponName;
    $("buildAttack").value = state.weaponAttack;
    state.selectedBuildId = detail.build.id;
    saveSession();

    for (let i = 1; i <= 6; i++) {
      const slot = detail.slots.find((s) => s.position === i);
      $(`buildRelic${i}`).value = slot ? String(slot.relic.id) : "";
    }
  } catch (error) {
    showError(error);
  }
}

function clearBuildEditor() {
  $("buildEditorTitle").textContent = "新規保存";
  $("buildId").value = "";
  $("buildName").value = "";
  $("buildCharacter").value = "";
  $("buildWeapon").value = "";
  $("buildAttack").value = state.weaponAttack;
  for (let i = 1; i <= 6; i++) {
    $(`buildRelic${i}`).value = "";
  }
}

async function loadCalcBuilds() {
  try {
    if (!state.relics.length) {
      state.relics = await Api.getRelics();
    }

    state.builds = await Api.getBuilds();
    $("calcBuild").innerHTML = state.builds
      .map((b) => `<option value="${b.id}">${b.id}: ${escapeHtml(b.name)}</option>`)
      .join("");
    $("calcAttack").value = state.weaponAttack;
    if (state.selectedBuildId) {
      $("calcBuild").value = String(state.selectedBuildId);
    }
    if ($("calcBuild").value) {
      await calculate();
    }
  } catch (error) {
    showError(error);
  }
}

async function calculate() {
  try {
    const buildId = Number($("calcBuild").value);
    const weaponAttack = Number($("calcAttack").value);
    if (!buildId) {
      alert("ビルドを選択してください。");
      return;
    }

    state.weaponAttack = weaponAttack;
    state.selectedBuildId = buildId;
    saveSession();

    const levelOverrides = {};
    Object.entries(state.levelOverrides).forEach(([key, value]) => {
      levelOverrides[Number(key)] = Number(value);
    });

    const result = await Api.calculate({ buildId, weaponAttack, levelOverrides });
    $("resBase").textContent = formatNum(result.baseAttack);
    $("resMult").textContent = `× ${formatNum(result.totalMultiplier)}`;
    $("resFinal").textContent = formatNum(result.finalAttack);
    $("resCount").textContent =
      `適用 ${result.appliedEffects.length} / 無効 ${result.ignoredEffects.length}`;

    renderStagedControls(result.stagedControls);

    $("appliedList").innerHTML = result.appliedEffects
      .map((e) => `<li>[${escapeHtml(e.category)}] EffectId=${e.effectId} Lv${e.level} ${escapeHtml(e.name)} ×${e.value}</li>`)
      .join("");
    $("ignoredList").innerHTML = result.ignoredEffects
      .map((e) => `<li>[${escapeHtml(e.category)}] EffectId=${e.effectId} Lv${e.level} ${escapeHtml(e.name)} ×${e.value}</li>`)
      .join("");
    $("calcLog").textContent = (result.logs || [])
      .map((l) => `[${l.step}] ${l.description}${l.multiplier == null ? "" : ` (×${l.multiplier})`} → ${formatNum(l.currentAttack)}`)
      .join("\n");
  } catch (error) {
    showError(error);
  }
}

function formatNum(value) {
  return Number(value).toLocaleString(undefined, { maximumFractionDigits: 8 });
}

function escapeHtml(text) {
  return String(text ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function buildSlotEditors() {
  $("relicEffectSlots").innerHTML = [1, 2, 3].map((i) =>
    `<label>Effect${i} <select id="relicEffect${i}"></select></label>`).join("");
  $("buildRelicSlots").innerHTML = [1, 2, 3, 4, 5, 6].map((i) =>
    `<label>遺物${i} <select id="buildRelic${i}"></select></label>`).join("");
}

function wireEvents() {
  document.querySelectorAll(".nav-btn").forEach((btn) => {
    btn.addEventListener("click", () => showView(btn.dataset.view));
  });

  $("btnCalculate").addEventListener("click", calculate);
  $("btnReloadBuildsCalc").addEventListener("click", loadCalcBuilds);
  $("calcBuild").addEventListener("change", () => {
    state.selectedBuildId = Number($("calcBuild").value) || null;
    saveSession();
    calculate();
  });
  $("calcAttack").addEventListener("change", () => {
    state.weaponAttack = Number($("calcAttack").value) || 0;
    saveSession();
  });

  $("btnEffectSearch").addEventListener("click", () => {
    const q = $("effectSearch").value.trim();
    loadEffects(q ? { q } : {});
  });
  $("btnEffectReload").addEventListener("click", () => loadEffects());
  $("effectCategory").addEventListener("change", () => loadEffects());
  $("btnEffectNew").addEventListener("click", () => openEffectDialog(null));
  $("btnEffectCancel").addEventListener("click", () => $("effectDialog").close());

  $("effectTable").addEventListener("click", async (event) => {
    const editId = event.target.dataset.edit;
    const delId = event.target.dataset.del;
    if (editId) {
      const effect = state.effects.find((e) => e.id === Number(editId));
      openEffectDialog(effect);
    }
    if (delId && confirm("削除しますか？")) {
      try {
        await Api.deleteEffect(Number(delId));
        await loadEffects();
      } catch (error) {
        showError(error);
      }
    }
  });

  $("effectForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const id = $("effRowId").value;
    const body = {
      id: id ? Number(id) : 0,
      effectId: Number($("effEffectId").value),
      name: $("effName").value.trim(),
      category: $("effCategory").value.trim(),
      canStack: $("effCanStack").checked,
      value: Number($("effValue").value),
      level: Number($("effLevel").value),
      description: $("effDescription").value.trim(),
      displayOrder: Number($("effDisplayOrder").value)
    };

    try {
      if (id) {
        await Api.updateEffect(Number(id), body);
      } else {
        await Api.createEffect(body);
      }
      $("effectDialog").close();
      await loadEffects();
    } catch (error) {
      showError(error);
    }
  });

  $("btnRelicSearch").addEventListener("click", loadRelics);
  $("btnRelicReload").addEventListener("click", async () => {
    $("relicSearch").value = "";
    $("relicColorFilter").value = "";
    await loadRelics();
  });
  $("relicTable").addEventListener("click", (event) => {
    const row = event.target.closest("tr[data-id]");
    if (!row) return;
    document.querySelectorAll("#relicTable tr").forEach((tr) => tr.classList.remove("selected"));
    row.classList.add("selected");
    loadRelicDetail(Number(row.dataset.id));
  });
  $("btnRelicClear").addEventListener("click", clearRelicEditor);
  $("btnRelicDelete").addEventListener("click", async () => {
    const id = $("relicId").value;
    if (!id) {
      alert("削除する遺物を選択してください。");
      return;
    }
    if (!confirm("削除しますか？")) return;
    try {
      await Api.deleteRelic(Number(id));
      clearRelicEditor();
      await loadRelics();
    } catch (error) {
      showError(error);
    }
  });
  $("relicForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const id = $("relicId").value;
    const effectIdsBySlot = [1, 2, 3].map((i) => {
      const value = $(`relicEffect${i}`).value;
      return value ? Number(value) : null;
    });
    const body = {
      id: id ? Number(id) : null,
      name: $("relicName").value.trim(),
      color: Number($("relicColor").value),
      memo: $("relicMemo").value.trim(),
      effectIdsBySlot
    };

    try {
      if (id) {
        await Api.updateRelic(Number(id), body);
      } else {
        await Api.createRelic(body);
      }
      clearRelicEditor();
      await loadRelics();
    } catch (error) {
      showError(error);
    }
  });

  $("btnBuildSearch").addEventListener("click", () => loadBuilds($("buildSearch").value.trim()));
  $("btnBuildReload").addEventListener("click", () => {
    $("buildSearch").value = "";
    loadBuilds();
  });
  $("buildTable").addEventListener("click", (event) => {
    const row = event.target.closest("tr[data-id]");
    if (!row) return;
    document.querySelectorAll("#buildTable tr").forEach((tr) => tr.classList.remove("selected"));
    row.classList.add("selected");
    loadBuildDetail(Number(row.dataset.id));
  });
  $("btnBuildClear").addEventListener("click", clearBuildEditor);
  $("buildAttack").addEventListener("change", () => {
    state.weaponAttack = Number($("buildAttack").value) || 0;
    saveSession();
  });
  $("btnBuildToCalc").addEventListener("click", () => {
    state.weaponAttack = Number($("buildAttack").value) || 0;
    const id = $("buildId").value;
    state.selectedBuildId = id ? Number(id) : state.selectedBuildId;
    saveSession();
    showView("calc");
  });
  $("btnBuildDelete").addEventListener("click", async () => {
    const id = $("buildId").value;
    if (!id) {
      alert("削除するビルドを選択してください。");
      return;
    }
    if (!confirm("削除しますか？")) return;
    try {
      await Api.deleteBuild(Number(id));
      if (state.selectedBuildId === Number(id)) {
        state.selectedBuildId = null;
        saveSession();
      }
      clearBuildEditor();
      await loadBuilds();
    } catch (error) {
      showError(error);
    }
  });
  $("buildForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const id = $("buildId").value;
    const relicIdsByPosition = [1, 2, 3, 4, 5, 6].map((i) => {
      const value = $(`buildRelic${i}`).value;
      return value ? Number(value) : null;
    });
    const body = {
      id: id ? Number(id) : null,
      name: $("buildName").value.trim(),
      characterName: $("buildCharacter").value.trim(),
      weaponName: $("buildWeapon").value.trim(),
      relicIdsByPosition
    };

    try {
      state.weaponAttack = Number($("buildAttack").value) || 0;
      const result = await Api.saveBuild(body);
      const savedId = result?.build?.id ?? Number(id);
      state.selectedBuildId = savedId;
      saveSession();
      await loadBuilds();
      await loadBuildDetail(savedId);
      alert(`保存しました。Id=${savedId}`);
    } catch (error) {
      showError(error);
    }
  });
}

async function boot() {
  buildSlotEditors();
  wireEvents();
  saveSession();
  $("calcAttack").value = state.weaponAttack;
  $("buildAttack").value = state.weaponAttack;

  try {
    state.effects = await Api.getEffects();
    state.relicEffects = await Api.getEffects({ forRelic: true });
    state.stagedDefs = await Api.getStagedEffects();
    state.relics = await Api.getRelics();
    fillEffectSlotOptions();
    fillBuildRelicOptions();
  } catch (error) {
    showError(error);
  }

  showView("calc");
}

boot();
