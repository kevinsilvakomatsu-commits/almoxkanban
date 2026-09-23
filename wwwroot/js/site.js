// ===== SIDEBAR TOGGLE MOBILE =====
document.addEventListener("DOMContentLoaded", function () {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const sidebarClose = document.getElementById('sidebarClose');

    function abrirSidebar() {
        document.body.classList.add('sidebar-open');
    }

    function fecharSidebar() {
        document.body.classList.remove('sidebar-open');
    }

    if (sidebarToggle) sidebarToggle.addEventListener('click', abrirSidebar);
    if (sidebarOverlay) sidebarOverlay.addEventListener('click', fecharSidebar);
    if (sidebarClose) sidebarClose.addEventListener('click', fecharSidebar);

    // Fechar sidebar ao clicar em link de navegacao no mobile
    document.querySelectorAll('.sidebar-item').forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth <= 768) {
                fecharSidebar();
            }
        });
    });

    // Fechar sidebar com tecla Escape
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.body.classList.contains('sidebar-open')) {
            fecharSidebar();
        }
    });

    // Inicializar previews de imagens
    inicializarPreviewsDeImagens();
    inicializarColarPrintScreen();
});

// Notificações e toasts globais
window.showToast = function (tipo, mensagem) {
    const container = document.getElementById("toast-container");
    if (!container) return;

    const toastId = "toast-" + Date.now();
    const bgClass = tipo === "sucesso"
        ? "bg-success text-white"
        : (tipo === "aviso"
            ? "bg-warning text-dark"
            : "bg-danger text-white");

    const icon = tipo === "sucesso"
        ? "bi-check-circle-fill"
        : (tipo === "aviso"
            ? "bi-exclamation-triangle-fill"
            : "bi-x-circle-fill");

    const html = `
        <div id="${toastId}" class="toast align-items-center ${bgClass} border-0 shadow" role="alert">
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center gap-2">
                    <i class="bi ${icon} fs-5"></i>
                    <span>${mensagem}</span>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML("beforeend", html);

    const toastEl = document.getElementById(toastId);
    const bsToast = new bootstrap.Toast(toastEl, {
        delay: 4000
    });

    bsToast.show();

    toastEl.addEventListener("hidden.bs.toast", () => {
        toastEl.remove();
    });
};

// Confirmação modal elegante
window.confirmarAcao = function (titulo, mensagem, callbackSim) {
    const modalEl = document.getElementById("modalConfirmacaoGlobal");

    if (!modalEl) {
        if (confirm(mensagem)) callbackSim();
        return;
    }

    document.getElementById("modalConfirmacaoTitulo").innerText = titulo;
    document.getElementById("modalConfirmacaoMensagem").innerText = mensagem;

    const btnSim = document.getElementById("modalConfirmacaoBtnSim");
    const novoBtnSim = btnSim.cloneNode(true);
    btnSim.parentNode.replaceChild(novoBtnSim, btnSim);

    const bsModal = new bootstrap.Modal(modalEl);

    novoBtnSim.addEventListener("click", () => {
        bsModal.hide();
        callbackSim();
    });

    bsModal.show();
};

// =======================================================
// PRÉ-VISUALIZAÇÃO DE FOTOS (Câmera, Arquivos e Galeria)
// =======================================================
function inicializarPreviewsDeImagens() {
    const inputsImagens = document.querySelectorAll('input[type="file"][accept*="image"]');
    
    inputsImagens.forEach(input => {
        let container = input.parentNode.querySelector(".image-preview-grid");
        if (!container) {
            container = document.createElement("div");
            container.className = "image-preview-grid mt-2";
            input.parentNode.appendChild(container);
        }

        input.addEventListener("change", function () {
            renderizarPreview(this, container);
        });
    });
}

function renderizarPreview(input, container) {
    container.innerHTML = "";
    const files = Array.from(input.files || []);
    if (!files.length) return;

    files.forEach(file => {
        if (!file.type.startsWith("image/")) return;

        const reader = new FileReader();
        reader.onload = function (e) {
            const card = document.createElement("div");
            card.className = "preview-thumbnail-card";
            card.innerHTML = `
                <img src="${e.target.result}" alt="${file.name}" class="preview-thumbnail-img" />
                <span class="preview-thumbnail-caption" title="${file.name}">${file.name}</span>
            `;
            container.appendChild(card);
        };
        reader.readAsDataURL(file);
    });
}

// =======================================================
// SUPORTE A COLAR PRINT SCREEN (CTRL + V)
// =======================================================
function inicializarColarPrintScreen() {
    const inputPrincipal = document.querySelector('input[name="FotoPrincipal"]') || document.querySelector('input[name="FotosResolucao"]');
    if (!inputPrincipal) return;

    document.addEventListener("paste", function (event) {
        const items = event.clipboardData?.items;
        if (!items) return;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            if (!item.type.startsWith("image/")) continue;

            const blob = item.getAsFile();
            if (!blob) continue;

            const arquivo = new File([blob], `captura_${Date.now()}.png`, { type: blob.type });
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(arquivo);
            inputPrincipal.files = dataTransfer.files;

            let container = inputPrincipal.parentNode.querySelector(".image-preview-grid");
            if (container) {
                renderizarPreview(inputPrincipal, container);
            }

            if (window.showToast) {
                showToast("sucesso", "Imagem colada da área de transferência com sucesso!");
            }
            break;
        }
    });
}
