
/* V.E.M.O.S. - Búsqueda pública y registro desde el footer.
   Se carga después de site.js y reutiliza sus popups de login y registro.
   No reemplaza las consultas al mapa ni la lógica de C# / SQL Server.
*/
document.addEventListener('DOMContentLoaded', function () {
    const guestSearch = document.getElementById('guest-search-form');
    const headerSearch = document.querySelector('.topbar .search-form');
    const footerForm = document.getElementById('footer-register-form');
    const footerEmail = document.getElementById('footer-email');
    const registerModal = document.getElementById('register-modal');
    const emailRegistro = registerModal ? registerModal.querySelector('input[name="email"]') : null;
    const esVisitante = !!document.querySelector('.topbar .auth-links');

    const storageEmailKey = 'vemos.registro.email';
    const storageOpenKey = 'vemos.registro.abrir';

    // Abrir el registro reutilizando los eventos del popup existente (site.js).
    function abrirRegistro(email) {
        if (!registerModal) return false;

        if (emailRegistro && email) {
            emailRegistro.value = email;
            emailRegistro.dispatchEvent(new Event('input', { bubbles: true }));
        }

        const boton = document.querySelector('.auth-links [data-auth-modal="register-modal"]')
            || document.querySelector('.signup-section [data-auth-modal="register-modal"]');

        if (boton) {
            boton.click();
        } else {
            // Respaldo para páginas que incluyan el modal, pero no su botón en el header.
            document.querySelectorAll('.auth-modal-backdrop').forEach(function (modal) {
                modal.classList.remove('is-open');
                modal.setAttribute('aria-hidden', 'true');
            });

            registerModal.classList.add('is-open');
            registerModal.setAttribute('aria-hidden', 'false');
        }

        return true;
    }

    // Si el visitante llegó desde otra página, continuar con su email.
    try {
        if (sessionStorage.getItem(storageOpenKey) === '1' && registerModal) {
            const emailGuardado = sessionStorage.getItem(storageEmailKey) || '';

            sessionStorage.removeItem(storageOpenKey);
            sessionStorage.removeItem(storageEmailKey);
            abrirRegistro(emailGuardado);
        }
    } catch (_) {
        // Algunos navegadores bloquean sessionStorage; no debe impedir el registro.
    }

    // El buscador grande funciona con Enter y con la flecha.
    if (guestSearch) {
        guestSearch.addEventListener('submit', function (event) {
            event.preventDefault();
            abrirRegistro();
        });
    }

    // El buscador del header solo pide registro a visitantes.
    if (headerSearch && esVisitante) {
        headerSearch.addEventListener('submit', function (event) {
            event.preventDefault();

            if (!abrirRegistro()) {
                window.location.assign(footerForm?.dataset.registerUrl || '/');
            }
        });
    }

    // Registro desde el formulario del footer.
    if (footerForm && footerEmail) {
        footerForm.addEventListener('submit', function (event) {
            event.preventDefault();

            footerEmail.value = footerEmail.value.trim();

            // Verifica que el correo tenga un formato válido.
            if (!footerEmail.reportValidity()) return;

            const correo = footerEmail.value;

            // Abre el popup con el correo ya completado.
            if (abrirRegistro(correo)) return;

            if (!esVisitante) {
                alert('Ya tenés una sesión iniciada. Esta opción inicia el registro de cuentas, no una suscripción por correo.');
                return;
            }

            try {
                sessionStorage.setItem(storageEmailKey, correo);
                sessionStorage.setItem(storageOpenKey, '1');
            } catch (_) {
                // Si el almacenamiento está bloqueado, continúa sin guardar el correo.
            }

            window.location.assign(footerForm.dataset.registerUrl || '/');
        });
    }
});