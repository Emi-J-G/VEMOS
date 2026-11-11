function isValidName(value) {
    return /^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+$/.test(value);
}

function isValidEmail(value) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function validateRegisterForm() {
    const nombreUsuario = document.getElementById("nombreUsuario").value.trim();
    const nombre = document.getElementById("nombre").value.trim();
    const apellido = document.getElementById("apellido").value.trim();
    const contrasena = document.getElementById("contrasena").value.trim();
    const email = document.getElementById("email").value.trim();
    const tipo = document.getElementById("tipo").value;

    if (!nombreUsuario || !nombre || !apellido || !contrasena || !email || !tipo) {
        alert("Por favor, complete todos los campos.");
        return false;
    }

    if (nombreUsuario.length < 3 || nombreUsuario.length > 20) {
        alert("El nombre de usuario debe tener entre 3 y 20 caracteres.");
        return false;
    }

    if (nombre.length < 3 || nombre.length > 20) {
        alert("El nombre debe tener entre 3 y 20 caracteres.");
        return false;
    }

    if (!isValidName(nombre)) {
        alert("El nombre no puede contener espacios ni caracteres especiales.");
        return false;
    }

    if (apellido.length < 3 || apellido.length > 20) {
        alert("El apellido debe tener entre 3 y 20 caracteres.");
        return false;
    }

    if (!isValidName(apellido)) {
        alert("El apellido no puede contener espacios ni caracteres especiales.");
        return false;
    }

    if (contrasena.length < 5 || contrasena.length > 15) {
        alert("La contraseña debe tener entre 5 y 15 caracteres.");
        return false;
    }

    if (!isValidEmail(email)) {
        alert("Ingrese un mail válido.");
        return false;
    }

    return true;
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-search-page]').forEach(function (searchBar) {
        searchBar.addEventListener('click', function (event) {
            event.preventDefault();
            window.location.assign(searchBar.getAttribute('data-search-page') || '/Buscar');
        });
    });

    const modalLogin = document.getElementById('login-modal');
    const modalRegister = document.getElementById('register-modal');
    const modalReport = document.getElementById('report-modal');
    const loginTriggers = document.querySelectorAll('[data-auth-modal="login-modal"]');
    const registerTriggers = document.querySelectorAll('[data-auth-modal="register-modal"]');
    const reportTriggers = document.querySelectorAll('[data-auth-modal="report-modal"]');
    const closeButtons = document.querySelectorAll('.auth-modal-close');
    const switchButtons = document.querySelectorAll('.auth-switch-trigger');

    function closeModal(modal) {
        if (!modal) return;
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
    }

    function closeAllModals() {
        closeModal(modalLogin);
        closeModal(modalRegister);
        closeModal(modalReport);
    }

    function openModal(modal) {
        if (!modal) return;
        closeAllModals();
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        const firstInput = modal.querySelector('input:not([type="hidden"]), select, textarea');
        if (firstInput) {
            setTimeout(() => firstInput.focus(), 40);
        }
    }

    loginTriggers.forEach(function (trigger) {
        trigger.addEventListener('click', function () {
            openModal(modalLogin);
        });
    });

    registerTriggers.forEach(function (trigger) {
        trigger.addEventListener('click', function () {
            openModal(modalRegister);
        });
    });

    reportTriggers.forEach(function (trigger) {
        trigger.addEventListener('click', function () {
            openModal(modalReport);
        });
    });

    closeButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            const modal = button.closest('.auth-modal-backdrop');
            closeModal(modal);
        });
    });

    switchButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            const target = button.getAttribute('data-auth-modal');
            if (target === 'login-modal') {
                openModal(modalLogin);
            }
            if (target === 'register-modal') {
                openModal(modalRegister);
            }
            if (target === 'report-modal') {
                openModal(modalReport);
            }
        });
    });

    [modalLogin, modalRegister, modalReport].forEach(function (modal) {
        if (!modal) return;

        modal.addEventListener('click', function (event) {
            if (event.target === modal) {
                closeModal(modal);
            }
        });
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllModals();
        }
    });

    const hasMessage = document.querySelector('.auth-banner-message');
    const openRegisterFromQuery = modalRegister && modalRegister.dataset.openRegister === 'true';
    if (openRegisterFromQuery) {
        openModal(modalRegister);
    } else if (hasMessage) {
        openModal(modalLogin);
    }

    function bindSmoothAnchors() {
        const anchors = document.querySelectorAll('a[href^="#"]');

        anchors.forEach(function (anchor) {
            anchor.addEventListener('click', function (event) {
                const targetSelector = anchor.getAttribute('href');
                if (!targetSelector || targetSelector === '#') {
                    return;
                }

                const target = document.querySelector(targetSelector);
                if (!target) {
                    return;
                }

                event.preventDefault();
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            });
        });
    }

    bindSmoothAnchors();

    const filterButton = document.getElementById('edit-filters-button');
    const filterPanel = document.getElementById('filter-editor-panel');
    const filterOptions = document.querySelectorAll('.filter-editor-option');
    const itemsContainer = document.querySelector('.filter-chips');

    if (!filterButton || !filterPanel || !itemsContainer) {
        return;
    }

    function closeFilterPanel() {
        filterPanel.hidden = true;
        filterPanel.setAttribute('aria-hidden', 'true');
        filterButton.setAttribute('aria-expanded', 'false');
    }

    function openFilterPanel() {
        filterPanel.hidden = false;
        filterPanel.setAttribute('aria-hidden', 'false');
        filterButton.setAttribute('aria-expanded', 'true');
    }

    function syncActiveFiltersDisplay() {
        const activeFilters = Array.from(filterOptions)
            .filter(option => option.classList.contains('is-selected'))
            .map(option => option.dataset.filterName);

        const chipNodes = Array.from(itemsContainer.querySelectorAll('.filter-chip'));
        const noFiltersChip = Array.from(itemsContainer.querySelectorAll('.filter-chip-no-filters'));

        chipNodes.forEach(node => node.remove());
        noFiltersChip.forEach(node => node.remove());

        if (activeFilters.length === 0) {
            const emptyChip = document.createElement('span');
            emptyChip.className = 'filter-chip filter-chip-no-filters';
            emptyChip.innerHTML = '<span aria-hidden="true">✓</span> Sin filtros activos';
            itemsContainer.insertBefore(emptyChip, filterButton);
            return;
        }

        activeFilters.forEach(name => {
            const chip = document.createElement('span');
            chip.className = 'filter-chip';
            chip.innerHTML = '<span aria-hidden="true">✓</span> ' + name;
            itemsContainer.insertBefore(chip, filterButton);
        });
    }

    filterButton.addEventListener('click', function () {
        const isOpen = filterButton.getAttribute('aria-expanded') === 'true';
        if (isOpen) {
            closeFilterPanel();
        } else {
            openFilterPanel();
        }
    });

    filterOptions.forEach(function (option) {
        option.addEventListener('click', async function () {
            const isSelected = option.classList.contains('is-selected');
            option.classList.toggle('is-selected', !isSelected);
            option.setAttribute('aria-pressed', String(!isSelected));

            const check = option.querySelector('.filter-editor-check');
            if (check) {
                check.textContent = !isSelected ? '✓' : '+';
            }

            syncActiveFiltersDisplay();
            const restricciones = Array.from(filterOptions)
                .filter(filter => filter.classList.contains('is-selected'))
                .map(filter => filter.dataset.filterName);
            const response = await fetch('/Options/Filtros/Guardar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(restricciones)
            });
            if (!response.ok) {
                option.classList.toggle('is-selected', isSelected);
                option.setAttribute('aria-pressed', String(isSelected));
                if (check) check.textContent = isSelected ? '✓' : '+';
                syncActiveFiltersDisplay();
            }
        });
    });

    syncActiveFiltersDisplay();
    closeFilterPanel();
});

const registerScript = document.querySelector('script[src*="site.js"]');
if (registerScript && !registerScript.dataset.filterScriptBound) {
    registerScript.dataset.filterScriptBound = 'true';
}