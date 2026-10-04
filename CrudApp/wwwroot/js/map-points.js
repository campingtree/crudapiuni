(() => {
    const mapElement = document.getElementById("map");
    if (!mapElement) return;

    const form = document.getElementById("point-form");
    const details = document.getElementById("point-details");
    const list = document.getElementById("point-list");
    const status = document.getElementById("status");
    const photo = document.getElementById("detail-photo");
    const map = L.map("map").setView([54.6872, 25.2797], 7);
    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
    const markers = L.layerGroup().addTo(map);
    let selectedPoint = null;
    let editingId = null;

    function showStatus(message, isError = false) {
        status.textContent = message;
        status.classList.toggle("error", isError);
    }

    async function api(path, options = {}) {
        const response = await fetch(path, options);
        if (!response.ok) {
            let message = `Request failed (${response.status}).`;
            const contentType = response.headers.get("content-type") || "";
            if (contentType.includes("json")) {
                const body = await response.json();
                message = body.detail || body.title || (body.errors && Object.values(body.errors).flat().join(" ")) || message;
            } else {
                const body = await response.text();
                if (body && body.length < 300) message = body;
            }
            throw new Error(message);
        }
        return response.status === 204 ? null : response.json();
    }

    function forecastText(forecast) {
        if (!forecast) return "Forecast pending";
        return `${forecast.date}: ${forecast.minC ?? "?"}–${forecast.maxC ?? "?"} °C, ` +
            `${forecast.precipitationProbability ?? "?"}% rain chance`;
    }

    async function loadPoints() {
        const points = await api("/api/points");
        list.replaceChildren();
        markers.clearLayers();
        if (points.length === 0) {
            const empty = document.createElement("p");
            empty.className = "muted";
            empty.textContent = "No points yet. Click the map or add a new point.";
            list.append(empty);
        }
        for (const point of points) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "point-item";
            const name = document.createElement("strong");
            name.textContent = `${point.isFavorite ? "★ " : ""}${point.name}`;
            const forecast = document.createElement("span");
            forecast.textContent = forecastText(point.forecast);
            button.append(name, forecast);
            button.addEventListener("click", () => selectPoint(point.id));
            list.append(button);
            L.marker([point.latitude, point.longitude]).on("click", () => selectPoint(point.id)).addTo(markers);
        }
    }

    async function selectPoint(id) {
        try {
            selectedPoint = await api(`/api/points/${id}`);
            editingId = null;
            form.hidden = true;
            details.hidden = false;
            document.getElementById("detail-name").textContent = selectedPoint.name;
            document.getElementById("detail-description").textContent = selectedPoint.description || "No description";
            document.getElementById("detail-meta").textContent =
                `${selectedPoint.latitude.toFixed(5)}, ${selectedPoint.longitude.toFixed(5)} · ` +
                `Priority ${selectedPoint.priority}${selectedPoint.isFavorite ? " · Favorite" : ""}` +
                `${selectedPoint.visitedOn ? ` · Visited ${selectedPoint.visitedOn}` : ""}`;
            document.getElementById("detail-forecast").textContent = `Tomorrow · ${forecastText(selectedPoint.forecast)}`;
            photo.hidden = !selectedPoint.photoUrl;
            if (selectedPoint.photoUrl) {
                photo.src = `${selectedPoint.photoUrl}?v=${Date.now()}`;
            } else {
                photo.removeAttribute("src");
            }
            map.panTo([selectedPoint.latitude, selectedPoint.longitude]);
            showStatus("");
        } catch (error) {
            showStatus(error.message, true);
        }
    }

    function openForm(point = null, coordinates = null) {
        editingId = point?.id || null;
        form.reset();
        document.getElementById("form-title").textContent = point ? "Edit point" : "New point";
        const center = coordinates || map.getCenter();
        form.elements.name.value = point?.name || "";
        form.elements.description.value = point?.description || "";
        form.elements.latitude.value = (point?.latitude ?? center.lat).toFixed(6);
        form.elements.longitude.value = (point?.longitude ?? center.lng).toFixed(6);
        form.elements.visitedOn.value = point?.visitedOn || "";
        form.elements.isFavorite.checked = point?.isFavorite || false;
        form.elements.priority.value = point?.priority ?? 0;
        document.getElementById("remove-photo").hidden = !point?.photoUrl;
        details.hidden = true;
        form.hidden = false;
        form.elements.name.focus();
    }

    map.on("click", event => openForm(null, event.latlng));
    document.getElementById("new-point").addEventListener("click", () => openForm());
    document.getElementById("edit-point").addEventListener("click", () => openForm(selectedPoint));
    document.getElementById("cancel-edit").addEventListener("click", () => {
        form.hidden = true;
        details.hidden = !selectedPoint;
    });

    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (!form.reportValidity()) return;
        const file = form.elements.photo.files[0];
        if (file && (file.size > 5 * 1024 * 1024 || !["image/jpeg", "image/png", "image/webp"].includes(file.type))) {
            showStatus("Choose a JPEG, PNG, or WebP image up to 5 MB.", true);
            return;
        }
        const request = {
            name: form.elements.name.value.trim(),
            description: form.elements.description.value.trim() || null,
            latitude: Number(form.elements.latitude.value),
            longitude: Number(form.elements.longitude.value),
            visitedOn: form.elements.visitedOn.value || null,
            isFavorite: form.elements.isFavorite.checked,
            priority: Number(form.elements.priority.value)
        };
        try {
            const point = await api(editingId ? `/api/points/${editingId}` : "/api/points", {
                method: editingId ? "PUT" : "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(request)
            });
            if (file) {
                const upload = new FormData();
                upload.append("file", file);
                await api(`/api/points/${point.id}/photo`, { method: "PUT", body: upload });
            }
            await loadPoints();
            await selectPoint(point.id);
            showStatus("Point saved.");
        } catch (error) {
            showStatus(error.message, true);
            await loadPoints().catch(() => {});
        }
    });

    document.getElementById("delete-point").addEventListener("click", async () => {
        if (!selectedPoint || !confirm(`Delete ${selectedPoint.name}?`)) return;
        try {
            await api(`/api/points/${selectedPoint.id}`, { method: "DELETE" });
            selectedPoint = null;
            details.hidden = true;
            photo.removeAttribute("src");
            await loadPoints();
            showStatus("Point deleted.");
        } catch (error) {
            showStatus(error.message, true);
        }
    });

    document.getElementById("remove-photo").addEventListener("click", async () => {
        if (!editingId) return;
        try {
            await api(`/api/points/${editingId}/photo`, { method: "DELETE" });
            document.getElementById("remove-photo").hidden = true;
            selectedPoint.photoUrl = null;
            showStatus("Photo removed.");
        } catch (error) {
            showStatus(error.message, true);
        }
    });

    loadPoints().catch(error => showStatus(error.message, true));
})();
