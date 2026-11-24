document.addEventListener('DOMContentLoaded', function () {
    const mapElement = document.getElementById('map');
    if (!mapElement || typeof L === 'undefined') return;

    const restaurantes = JSON.parse(mapElement.dataset.restaurantes || '[]');

    const map = L.map('map', {
        zoomControl: true,
        attributionControl: true
    }).setView([-34.6037, -58.3816], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    const markers = new Map();

    function normalizarRestriccion(value) {
        return (value || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .trim()
            .toLowerCase();
    }

    function getSelectedRestrictions() {
        return [...document.querySelectorAll('.map-filter-checkbox:checked')]
            .map(input => input.value);
    }

    function updateFilterSummary(selected) {
        const summary = document.getElementById('mapFilterSummary');
        summary.textContent = selected.length > 0
            ? `${selected.length} activas`
            : 'Sin filtros activos';
    }

    function createPopupContent(restaurante) {
        return `
            <div class="map-popup">
                <strong>${restaurante.Nombre}</strong>
                <span>${restaurante.Descripcion}</span>
                <small>${(restaurante.Restricciones || []).join(', ') || 'Sin restricciones cargadas'}</small>
                <div class="map-popup-actions">
                    <a class="map-popup-link" href="/Home/DetalleRestaurante?nombre=${encodeURIComponent(restaurante.Nombre)}">Ver información</a>
                </div>
            </div>`;
    }

    function ajustarMapa(restaurantesVisibles) {
        const puntos = restaurantesVisibles.map(restaurante => [restaurante.Lat, restaurante.Lng]);

        if (puntos.length === 1) {
            map.setView(puntos[0], 15);
        } else if (puntos.length > 1) {
            map.fitBounds(puntos, { padding: [35, 35], maxZoom: 15 });
        } else {
            map.setView([-34.6037, -58.3816], 12);
        }
    }

    function renderMarkers(textoBusqueda = '') {
        const selectedRestrictions = getSelectedRestrictions();
        updateFilterSummary(selectedRestrictions);

        markers.forEach(marker => map.removeLayer(marker));
        markers.clear();

        const texto = textoBusqueda.trim().toLowerCase();
        const restaurantesFiltrados = restaurantes.filter(restaurante => {
            const coincideRestriccion = selectedRestrictions.length === 0 || selectedRestrictions.every(restriccion =>
                (restaurante.Restricciones || []).some(disponible =>
                    normalizarRestriccion(disponible) === normalizarRestriccion(restriccion)));

            const coincideTexto = !texto ||
                (restaurante.Nombre || '').toLowerCase().includes(texto) ||
                (restaurante.Descripcion || '').toLowerCase().includes(texto);

            return coincideRestriccion && coincideTexto;
        });

        restaurantesFiltrados.forEach(restaurante => {
            const marker = L.marker([restaurante.Lat, restaurante.Lng])
                .bindPopup(createPopupContent(restaurante));

            marker.addTo(map);
            markers.set(`${restaurante.Nombre}-${restaurante.Lat}-${restaurante.Lng}`, marker);
        });

        ajustarMapa(restaurantesFiltrados);

        if (restaurantesFiltrados.length === 0) {
            const emptyMessage = L.popup()
                .setLatLng([-34.6037, -58.3816])
                .setContent('<div class="map-popup"><strong>No hay restaurantes</strong><br>Probá otra combinación de filtros.</div>');
            emptyMessage.addTo(map);
            setTimeout(() => emptyMessage.closePopup(), 2500);
        }
    }

    renderMarkers();

    document.querySelectorAll('.map-filter-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', () => renderMarkers(document.getElementById('mapSearch').value));
    });

    document.getElementById('mapSearch').addEventListener('input', function () {
        renderMarkers(this.value);
    });

    document.getElementById('locateBtn').addEventListener('click', function () {
        if (!navigator.geolocation) {
            alert('Tu navegador no soporta geolocalización.');
            return;
        }

        navigator.geolocation.getCurrentPosition(function (position) {
            const lat = position.coords.latitude;
            const lng = position.coords.longitude;

            L.marker([lat, lng])
                .addTo(map)
                .bindPopup('<div class="map-popup"><strong>Tu ubicación</strong></div>')
                .openPopup();

            map.setView([lat, lng], 15);
        }, function () {
            alert('No pudimos obtener tu ubicación.');
        });
    });
});
