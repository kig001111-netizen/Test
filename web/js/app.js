const RELIC_COLS = 6;
const MAX_PER_COL = 3;

const state = {
  weaponAttack: Number(localStorage.getItem("weaponAttack") || 1000),
  selectedBuildId: localStorage.getItem("selectedBuildId")
    ? Number(localStorage.getItem("selectedBuildId"))
    : null,
  effects: [],
  matrixEffects: [],
  builds: [],
  columns: Array.from({ length: RELIC_COLS }, () => []),
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

const titles = {
  calc: "火力計算",
  build: "ビルド管理",
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
  const buildText = state.selectedBuildId == null ? "新規" : `Build #${state.selectedBuildId}`;
  $("sessionInfo").textContent = `火力 ${state.weaponAttack}\n${buildText}`;
  if ($("calcBuildHint")) {
    $("calcBuildHint").textContent = `編集中: ${buildText}（チェックすると即計算）`;
  }
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
    renderMatrix();
    recalculate();
  } else if (name === "build") {
    loadBuilds();
  } else if (name === "effect") {
    loadEffects();
  }
}

function columnCount(colIndex) {
  return state.columns[colIndex].length;
}

function isChecked(effectRowId, colIndex) {
  return state.columns[colIndex].includes(effectRowId);
}

function toggleCell(effectRowId, colIndex, checked) {
  const col = state.columns[colIndex];
  const idx = col.indexOf(effectRowId);
  if (checked) {
    if (idx >= 0) return true;
    if (col.length >= MAX_PER_COL) {
      alert(`遺物${colIndex + 1} に設定できる効果は最大 ${MAX_PER_COL} 件です。`);
      return false;
    }
    col.push(effectRowId);
  } else if (idx >= 0) {
    col.splice(idx, 1);
  }
  return true;
}

function renderMatrix() {
  const body = $("matrixBody");
  if (!body) return;
  const stagedIds = new Set((state.stagedDefs || []).map((d) => d.effectId));
  body.innerHTML = state.matrixEffects.map((e) => {
    const label = stagedIds.has(e.effectId)
      ? `${e.effectId}: ${escapeHtml(e.name)}（段階）`
      : `${e.effectId}: ${escapeHtml(e.name)}`;
    const cells = Array.from({ length: RELIC_COLS }, (_, col) => {
      const on = isChecked(e.id, col);
      return `<td class="matrix-check"><input type="checkbox" data-effect-id="${e.id}" data-col="${col}" ${on ? "checked" : ""} /></td>`;
    }).join("");
    return `<tr><th class="matrix-sticky">${label}</th>${cells}</tr>`;
  }).join("");

  body.querySelectorAll("input[type=checkbox]").forEach((input) => {
    input.addEventListener("change", () => {
      const effectId = Number(input.dataset.effectId);
      const col = Number(input.dataset.col);
      const ok = toggleCell(effectId, col, input.checked);
      if (!ok) {
        input.checked = false;
        return;
      }
      recalculate();
    });
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
          <div class="value-hint">EffectId=${c.effectId}</div>
        </div>
        <select data-effect-id="${c.effectId}">${options}</select>
      </div>`;
  }).join("");

  host.querySelectorAll("select[data-effect-id]").forEach((select) => {
    select.addEventListener("change", () => {
      state.levelOverrides[Number(select.dataset.effectId)] = Number(select.value);
      persistLevelOverrides();
      recalculate();
    });
  });
}

async function recalculate() {
  try {
    state.weaponAttack = Number($("calcAttack").value) || 0;
    saveSession();
    const levelOverrides = {};
    Object.entries(state.levelOverrides).forEach(([k, v]) => {
      levelOverrides[Number(k)] = Number(v);
    });
    const result = await Api.calculateMatrix({
      weaponAttack: state.weaponAttack,
      effectIdsByRelic: state.columns.map((c) => c.slice()),
      levelOverrides
    });

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

function clearMatrix() {
  state.columns = Array.from({ length: RELIC_COLS }, () => []);
  state.selectedBuildId = null;
  $("calcBuildName").value = "";
  $("calcCharacter").value = "";
  $("calcWeapon").value = "";
  saveSession();
  renderMatrix();
  recalculate();
}

async function loadMatrixFromBuild(buildId) {
  const detail = await Api.loadBuildMatrix(buildId);
  if (!detail) {
    throw new Error("ビルドが見つかりません。");
  }
  state.selectedBuildId = detail.build.id;
  state.columns = Array.from({ length: RELIC_COLS }, (_, i) => (detail.effectIdsByRelic[i] || []).slice());
  $("calcBuildName").value = detail.build.name || "";
  $("calcCharacter").value = detail.build.characterName || "";
  $("calcWeapon").value = detail.build.weaponName || "";
  saveSession();
  renderMatrix();
  await recalculate();
}

async function saveCurrentBuild() {
  const name = $("calcBuildName").value.trim();
  if (!name) {
    alert("ビルド名を入力してください。");
    return;
  }
  try {
    const saved = await Api.saveBuildMatrix({
      id: state.selectedBuildId,
      name,
      characterName: $("calcCharacter").value.trim(),
      weaponName: $("calcWeapon").value.trim(),
      effectIdsByRelic: state.columns.map((c) => c.slice())
    });
    state.selectedBuildId = saved.build.id;
    saveSession();
    alert(`保存しました。Id=${saved.build.id}`);
  } catch (error) {
    showError(error);
  }
}

async function loadEffects(params = {}) {
  try {
    state.effects = await Api.getEffects(params);
    const categories = ["(すべて)", ...new Set(state.effects.map((e) => e.category).filter(Boolean))];
    const select = $("effectCategory");
    const current = select.value;
    select.innerHTML = categories.map((c) => `<option value="${c === "(すべて)" ? "" : c}">${c}</option>`).join("");
    if (categories.includes(current) || current === "") select.value = current;

    const category = select.value;
    const rows = category ? state.effects.filter((e) => e.category === category) : state.effects;
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
  } catch (error) {
    showError(error);
  }
}

function openEffectDialog(effect) {
  $("effectDialogTitle").textContent = effect ? "Effect編集" : "新規登録";
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

async function loadBuilds(q = "") {
  try {
    state.builds = await Api.getBuilds(q ? { q } : {});
    $("buildTable").innerHTML = state.builds.map((b) => `
      <tr data-id="${b.id}">
        <td>${b.id}</td>
        <td>${escapeHtml(b.name)}</td>
        <td>${escapeHtml(b.characterName)}</td>
        <td>${escapeHtml(b.weaponName)}</td>
        <td><button type="button" class="btn primary" data-open-calc="${b.id}">計算で開く</button></td>
      </tr>`).join("");
  } catch (error) {
    showError(error);
  }
}

async function openBuildMeta(id) {
  const detail = await Api.getBuild(id);
  if (!detail) {
    alert("ビルドが見つかりません。");
    return;
  }
  $("buildEditorTitle").textContent = `編集 Id=${detail.build.id}`;
  $("buildId").value = detail.build.id;
  $("buildName").value = detail.build.name;
  $("buildCharacter").value = detail.build.characterName;
  $("buildWeapon").value = detail.build.weaponName;
  $("buildDialog").showModal();
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

function wireEvents() {
  document.querySelectorAll(".nav-btn").forEach((btn) => {
    btn.addEventListener("click", () => showView(btn.dataset.view));
  });

  $("calcAttack").addEventListener("change", recalculate);
  $("calcAttack").addEventListener("input", () => {
    state.weaponAttack = Number($("calcAttack").value) || 0;
  });
  $("btnSaveBuild").addEventListener("click", saveCurrentBuild);
  $("btnClearMatrix").addEventListener("click", clearMatrix);

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
      openEffectDialog(state.effects.find((e) => e.id === Number(editId)));
    }
    if (delId && confirm("削除しますか？")) {
      try {
        await Api.deleteEffect(Number(delId));
        await refreshMatrixEffects();
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
      if (id) await Api.updateEffect(Number(id), body);
      else await Api.createEffect(body);
      $("effectDialog").close();
      await refreshMatrixEffects();
      await loadEffects();
      renderMatrix();
      await recalculate();
    } catch (error) {
      showError(error);
    }
  });

  $("btnBuildSearch").addEventListener("click", () => loadBuilds($("buildSearch").value.trim()));
  $("btnBuildReload").addEventListener("click", () => {
    $("buildSearch").value = "";
    loadBuilds();
  });
  $("buildTable").addEventListener("click", async (event) => {
    const openId = event.target.dataset.openCalc;
    if (openId) {
      try {
        await loadMatrixFromBuild(Number(openId));
        showView("calc");
      } catch (error) {
        showError(error);
      }
      return;
    }
    const row = event.target.closest("tr[data-id]");
    if (!row) return;
    openBuildMeta(Number(row.dataset.id));
  });
  $("btnBuildCancel").addEventListener("click", () => $("buildDialog").close());
  $("btnBuildOpenCalc").addEventListener("click", async () => {
    const id = Number($("buildId").value);
    if (!id) return;
    $("buildDialog").close();
    try {
      await loadMatrixFromBuild(id);
      showView("calc");
    } catch (error) {
      showError(error);
    }
  });
  $("btnBuildDelete").addEventListener("click", async () => {
    const id = $("buildId").value;
    if (!id) return;
    if (!confirm("削除しますか？")) return;
    try {
      await Api.deleteBuild(Number(id));
      if (state.selectedBuildId === Number(id)) {
        state.selectedBuildId = null;
        saveSession();
      }
      $("buildDialog").close();
      await loadBuilds();
    } catch (error) {
      showError(error);
    }
  });
  $("buildMetaForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const id = Number($("buildId").value);
    try {
      const matrix = await Api.loadBuildMatrix(id);
      await Api.saveBuildMatrix({
        id,
        name: $("buildName").value.trim(),
        characterName: $("buildCharacter").value.trim(),
        weaponName: $("buildWeapon").value.trim(),
        effectIdsByRelic: matrix.effectIdsByRelic
      });
      $("buildDialog").close();
      await loadBuilds();
      alert("保存しました。");
    } catch (error) {
      showError(error);
    }
  });
}

async function refreshMatrixEffects() {
  state.matrixEffects = await Api.getEffects({ forRelic: true });
  state.stagedDefs = await Api.getStagedEffects();
  state.effects = await Api.getEffects();
}

async function boot() {
  const status = $("bootStatus");
  const root = $("appRoot");
  try {
    if (status) status.textContent = "データベースを準備しています…";
    await Db.init();
  } catch (error) {
    if (status) status.textContent = "起動に失敗しました。ページを再読み込みしてください。";
    showError(error);
    return;
  }

  if (status) status.hidden = true;
  if (root) root.hidden = false;

  wireEvents();
  $("calcAttack").value = state.weaponAttack;
  saveSession();

  try {
    await refreshMatrixEffects();
    renderMatrix();
    if (state.selectedBuildId) {
      try {
        await loadMatrixFromBuild(state.selectedBuildId);
      } catch {
        state.selectedBuildId = null;
        saveSession();
        await recalculate();
      }
    } else {
      await recalculate();
    }
  } catch (error) {
    showError(error);
  }

  showView("calc");
}

boot();
