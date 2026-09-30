// ===== SIDEBAR TOGGLE MOBILE =====
document.addEventListener("DOMContentLoaded", function () {
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarOverlay = document.getElementById("sidebarOverlay");
    const sidebarClose = document.getElementById("sidebarClose");

    function abrirSidebar() {
        document.body.classList.add("sidebar-open");
    }

    function fecharSidebar() {
        document.body.classList.remove("sidebar-open");
    }

    if (sidebarToggle) sidebarToggle.addEventListener("click", abrirSidebar);
    if (sidebarOverlay) sidebarOverlay.addEventListener("click", fecharSidebar);
    if (sidebarClose) sidebarClose.addEventListener("click", fecharSidebar);

    // Fechar sidebar ao clicar em link de navegacao no mobile
    document.querySelectorAll(".sidebar-item").forEach(function (link) {
        link.addEventListener("click", function () {
            if (window.innerWidth <= 768) {
                fecharSidebar();
            }
        });
    });

    // Fechar sidebar com tecla Escape
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && document.body.classList.contains("sidebar-open")) {
            fecharSidebar();
        }
    });

    // Inicializar recursos de foto, câmera e formulários
    inicializarBotoesCameraEUpload();
    inicializarPreviewsDeImagens();
    inicializarColarPrintScreen();
    inicializarPrazoPadrao();
});

// Tratamento global para imagens quebradas (avatares, capas, miniaturas)
document.addEventListener("error", function (e) {
    if (e.target && e.target.tagName === "IMG") {
        if (e.target.classList.contains("user-avatar")) {
            e.target.style.display = "none";
            if (e.target.nextElementSibling) e.target.nextElementSibling.style.display = "flex";
        } else if (e.target.classList.contains("avatar-xs") || e.target.classList.contains("rounded-circle")) {
            e.target.style.display = "none";
            if (e.target.nextElementSibling) e.target.nextElementSibling.style.display = "inline-flex";
        } else if (e.target.classList.contains("card-img-cover")) {
            const link = e.target.closest(".card-img-link") || e.target.closest("a");
            if (link) link.style.display = "none";
            else e.target.style.display = "none";
        }
    }
}, true);

// =======================================================
// PRÉ-DEFINIÇÃO DE PRAZO (Amanhã + Horário Atual + 1 Hora)
// =======================================================
function inicializarPrazoPadrao() {
    const inputData = document.getElementById("DataPrazo");
    const inputHora = document.getElementById("HoraPrazo");
    if (!inputData || !inputHora) return;

    // Se estiver em modo de criação (sem dados salvos ou vazio), pré-define data para amanhã
    if (!inputData.value) {
        const amanha = new Date();
        amanha.setDate(amanha.getDate() + 1);
        const yyyy = amanha.getFullYear();
        const mm = String(amanha.getMonth() + 1).padStart(2, "0");
        const dd = String(amanha.getDate()).padStart(2, "0");
        inputData.value = `${yyyy}-${mm}-${dd}`;
    }

    // Se o horário estiver vazio, pré-define para o horário atual + 1 hora
    if (!inputHora.value) {
        const agoraMaisUmaHora = new Date();
        agoraMaisUmaHora.setHours(agoraMaisUmaHora.getHours() + 1);
        const hh = String(agoraMaisUmaHora.getHours()).padStart(2, "0");
        const min = String(agoraMaisUmaHora.getMinutes()).padStart(2, "0");
        inputHora.value = `${hh}:${min}`;
    }
}

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
// BOTÕES DE CÂMERA DIRETA E GALERIA / ARQUIVOS
// =======================================================
// Dicionário em memória para gerenciar DataTransfer de múltiplos arquivos
const uploadStateMap = new Map();

function getUploadDataTransfer(input) {
    if (!uploadStateMap.has(input)) {
        const dt = new DataTransfer();
        if (input.files) {
            Array.from(input.files).forEach(f => dt.items.add(f));
        }
        uploadStateMap.set(input, dt);
    }
    return uploadStateMap.get(input);
}

function inicializarBotoesCameraEUpload() {
    const actionButtons = document.querySelectorAll("[data-target-input]");

    actionButtons.forEach(btn => {
        const targetId = btn.getAttribute("data-target-input");
        const mode = btn.getAttribute("data-mode"); // "camera" ou "gallery"
        const targetInput = document.getElementById(targetId);
        if (!targetInput) return;

        btn.addEventListener("click", function () {
            if (mode === "camera") {
                // Cria ou recupera o input de câmera com capture="environment" (abre a câmera direto no celular)
                let camInput = document.getElementById(targetId + "_camTrigger");
                if (!camInput) {
                    camInput = document.createElement("input");
                    camInput.type = "file";
                    camInput.accept = "image/*";
                    camInput.setAttribute("capture", "environment");
                    camInput.id = targetId + "_camTrigger";
                    camInput.style.display = "none";
                    document.body.appendChild(camInput);

                    camInput.addEventListener("change", function () {
                        if (this.files && this.files.length > 0) {
                            adicionarArquivosAoInput(targetInput, Array.from(this.files));
                            this.value = ""; // Limpa para permitir tirar foto novamente
                        }
                    });
                }
                camInput.click();
            } else {
                // Modo galeria / arquivo: aciona o seletor padrão do sistema sem forçar a câmera
                targetInput.click();
            }
        });
    });
}

function adicionarArquivosAoInput(targetInput, novosArquivos) {
    const isMultiple = targetInput.hasAttribute("multiple");
    const dt = isMultiple ? getUploadDataTransfer(targetInput) : new DataTransfer();

    if (!isMultiple) {
        // Se for input único (ex: foto principal), limpa o anterior e define o novo
        uploadStateMap.set(targetInput, dt);
    }

    novosArquivos.forEach(file => {
        dt.items.add(file);
    });

    targetInput.files = dt.files;

    // Atualizar preview
    const previewContainer = document.getElementById("preview-" + targetInput.id) ||
                             targetInput.parentNode.querySelector(".image-preview-grid");
    if (previewContainer) {
        renderizarPreview(targetInput, previewContainer);
    }

    if (window.showToast) {
        const msg = novosArquivos.length === 1
            ? "Foto capturada com sucesso!"
            : `${novosArquivos.length} fotos adicionadas com sucesso!`;
        showToast("sucesso", msg);
    }
}

function inicializarPreviewsDeImagens() {
    const inputsImagens = document.querySelectorAll('input[type="file"][accept*="image"]');

    inputsImagens.forEach(input => {
        let container = document.getElementById("preview-" + input.id) ||
                        input.parentNode.querySelector(".image-preview-grid");
        if (!container) {
            container = document.createElement("div");
            container.className = "image-preview-grid mt-2";
            container.id = "preview-" + input.id;
            input.parentNode.appendChild(container);
        }

        input.addEventListener("change", function () {
            // Sincroniza o DataTransfer do input
            const dt = new DataTransfer();
            Array.from(this.files || []).forEach(f => dt.items.add(f));
            uploadStateMap.set(this, dt);

            renderizarPreview(this, container);
        });
    });
}

function renderizarPreview(input, container) {
    container.innerHTML = "";
    const dt = getUploadDataTransfer(input);
    const files = Array.from(dt.files || []);
    if (!files.length) return;

    files.forEach((file, index) => {
        if (!file.type.startsWith("image/")) return;

        const reader = new FileReader();
        reader.onload = function (e) {
            const card = document.createElement("div");
            card.className = "preview-thumbnail-card";
            card.innerHTML = `
                <button type="button" class="preview-thumbnail-remove" title="Remover esta foto" data-index="${index}">
                    <i class="bi bi-x"></i>
                </button>
                <img src="${e.target.result}" alt="${file.name}" class="preview-thumbnail-img" />
                <span class="preview-thumbnail-caption" title="${file.name}">${file.name}</span>
            `;

            // Ação do botão [x] para remover a imagem individualmente
            card.querySelector(".preview-thumbnail-remove").addEventListener("click", function (ev) {
                ev.preventDefault();
                ev.stopPropagation();
                removerArquivoDoInput(input, index, container);
            });

            container.appendChild(card);
        };
        reader.readAsDataURL(file);
    });
}

function removerArquivoDoInput(input, indexParaRemover, container) {
    const dtAtual = getUploadDataTransfer(input);
    const novoDt = new DataTransfer();

    Array.from(dtAtual.files).forEach((file, idx) => {
        if (idx !== indexParaRemover) {
            novoDt.items.add(file);
        }
    });

    uploadStateMap.set(input, novoDt);
    input.files = novoDt.files;
    renderizarPreview(input, container);

    if (window.showToast) {
        showToast("aviso", "Foto removida.");
    }
}

// =======================================================
// SUPORTE A COLAR PRINT SCREEN (CTRL + V)
// =======================================================
function inicializarColarPrintScreen() {
    const inputPrincipal = document.querySelector('input[name="FotoPrincipal"]') ||
                           document.querySelector('input[name="FotosResolucao"]') ||
                           document.querySelector('input[name="NovaFotoPrincipal"]');
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
            adicionarArquivosAoInput(inputPrincipal, [arquivo]);

            if (window.showToast) {
                showToast("sucesso", "Imagem colada da área de transferência com sucesso!");
            }
            break;
        }
    });
}