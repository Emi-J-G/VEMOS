document.addEventListener("DOMContentLoaded", function () {
    /** @type {HTMLDivElement|null} */
    const messages = document.getElementById("chat-messages");

    /** @type {HTMLFormElement|null} */
    const compose = document.getElementById("chat-compose");

    if (!messages) {
        return;
    }

    /**
     * @typedef {Object} MensajeChatRespuesta
     * @property {number} id
     * @property {number} idConversacion
     * @property {number} idRemitente
     * @property {string} texto
     * @property {string} fecha
     * @property {string} remitenteNombre
     */

    /** @type {string} */
    const usuarioActual = messages.dataset.usuario || "";

    /** @type {number} */
    const conversacionActual = Number(messages.dataset.conversacion || "0");

    /**
     * @param {string} fechaTexto
     * @returns {string}
     */
    function formatearHora(fechaTexto) {
        const fecha = new Date(fechaTexto);
        if (Number.isNaN(fecha.getTime())) {
            return "";
        }

        return fecha.toLocaleTimeString([], {
            timeZone: "America/Argentina/Buenos_Aires",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    /**
     * @param {MensajeChatRespuesta[]} items
     * @returns {void}
     */
    function renderizarMensajes(items) {
        messages.replaceChildren();

        if (items.length === 0) {
            const empty = document.createElement("p");
            empty.className = "chat-empty chat-empty-messages";
            empty.textContent = "Este chat está listo. Escribí el primer mensaje.";
            messages.appendChild(empty);
        }

        items.forEach(function (mensaje) {
            const propio = usuarioActual === mensaje.remitenteNombre;

            const contenedor = document.createElement("div");
            contenedor.className = propio ? "chat-message own" : "chat-message";

            const texto = document.createElement("p");
            texto.textContent = mensaje.texto;

            const hora = document.createElement("time");
            hora.textContent = formatearHora(mensaje.fecha);

            contenedor.appendChild(texto);
            contenedor.appendChild(hora);
            messages.appendChild(contenedor);
        });

        messages.scrollTop = messages.scrollHeight;
    }

    /** @returns {Promise<void>} */
    async function cargarMensajes() {
        const response = await fetch(`/Chat/Mensajes?idConversacion=${encodeURIComponent(conversacionActual)}`);
        if (!response.ok) {
            return;
        }

        /** @type {MensajeChatRespuesta[]} */
        const items = await response.json();
        renderizarMensajes(items);
    }

    messages.scrollTop = messages.scrollHeight;

    if (compose) {
        compose.addEventListener("submit", async function (event) {
            event.preventDefault();

            /** @type {HTMLFormElement} */
            const form = event.currentTarget;

            /** @type {HTMLInputElement|null} */
            const input = document.getElementById("chat-text");

            /** @type {HTMLParagraphElement|null} */
            const feedback = document.getElementById("chat-feedback");

            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form)
            });

            if (response.ok) {
                if (input) {
                    input.value = "";
                }

                if (feedback) {
                    feedback.hidden = true;
                }

                await cargarMensajes();
            } else {
                const data = await response.json().catch(function () {
                    return {};
                });

                if (feedback) {
                    feedback.textContent = data.message || "No se pudo enviar el mensaje.";
                    feedback.hidden = false;
                }
            }
        });
    }

    window.setInterval(cargarMensajes, 3000);
});
