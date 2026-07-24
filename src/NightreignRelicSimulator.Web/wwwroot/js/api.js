const Api = {
  async request(path, options = {}) {
    const response = await fetch(path, {
      headers: {
        "Content-Type": "application/json",
        ...(options.headers || {})
      },
      ...options
    });

    if (response.status === 204) {
      return null;
    }

    const text = await response.text();
    const data = text ? JSON.parse(text) : null;

    if (!response.ok) {
      const message = data?.message || `HTTP ${response.status}`;
      throw new Error(message);
    }

    return data;
  },

  getEffects(params = {}) {
    const q = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        q.set(key, String(value));
      }
    });
    const suffix = q.toString() ? `?${q}` : "";
    return this.request(`/api/effects${suffix}`);
  },

  getStagedEffects() {
    return this.request("/api/effects/staged");
  },

  createEffect(body) {
    return this.request("/api/effects", { method: "POST", body: JSON.stringify(body) });
  },

  updateEffect(id, body) {
    return this.request(`/api/effects/${id}`, { method: "PUT", body: JSON.stringify(body) });
  },

  deleteEffect(id) {
    return this.request(`/api/effects/${id}`, { method: "DELETE" });
  },

  getRelics(params = {}) {
    const q = new URLSearchParams(params);
    const suffix = q.toString() ? `?${q}` : "";
    return this.request(`/api/relics${suffix}`);
  },

  getRelic(id) {
    return this.request(`/api/relics/${id}`);
  },

  createRelic(body) {
    return this.request("/api/relics", { method: "POST", body: JSON.stringify(body) });
  },

  updateRelic(id, body) {
    return this.request(`/api/relics/${id}`, { method: "PUT", body: JSON.stringify(body) });
  },

  deleteRelic(id) {
    return this.request(`/api/relics/${id}`, { method: "DELETE" });
  },

  getBuilds(params = {}) {
    const q = new URLSearchParams(params);
    const suffix = q.toString() ? `?${q}` : "";
    return this.request(`/api/builds${suffix}`);
  },

  getBuild(id) {
    return this.request(`/api/builds/${id}`);
  },

  saveBuild(body) {
    if (body.id) {
      return this.request(`/api/builds/${body.id}`, {
        method: "PUT",
        body: JSON.stringify(body)
      });
    }

    return this.request("/api/builds", { method: "POST", body: JSON.stringify(body) });
  },

  deleteBuild(id) {
    return this.request(`/api/builds/${id}`, { method: "DELETE" });
  },

  calculate(body) {
    return this.request("/api/calculate", { method: "POST", body: JSON.stringify(body) });
  }
};
