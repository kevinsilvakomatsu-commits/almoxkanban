// Notificações e utilitários globais
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
                <button type="button" class="btn-close btn-close-white me-2 m-auto"
                        data-bs-dismiss="toast"></button>
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
        if (confirm(mensagem))
            callbackSim();

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
// COLAR PRINT SCREEN (CTRL + V)
// =======================================================

document.addEventListener("DOMContentLoaded", function () {

    const inputPrincipal =
        document.querySelector('input[name="FotoPrincipal"]');

    if (!inputPrincipal)
        return;

    criarPreview(inputPrincipal);

    document.addEventListener("paste", function (event) {

        const items = event.clipboardData?.items;

        if (!items)
            return;

        for (let i = 0; i < items.length; i++) {

            const item = items[i];

            if (!item.type.startsWith("image/"))
                continue;

            const blob = item.getAsFile();

            if (!blob)
                continue;

            const arquivo = new File(
                [blob],
                "printscreen.png",
                {
                    type: blob.type
                });

            const dataTransfer = new DataTransfer();

            dataTransfer.items.add(arquivo);

            inputPrincipal.files = dataTransfer.files;

            exibirPreviewImagem(inputPrincipal, arquivo);

            if (window.showToast) {
                showToast(
                    "sucesso",
                    "Imagem colada com sucesso."
                );
            }

            break;
        }
    });
});


function criarPreview(input) {

    const preview = document.createElement("div");

    preview.id = "preview-foto-principal";

    preview.style.marginTop = "10px";

    input.parentNode.appendChild(preview);

    input.addEventListener("change", function () {

        if (!this.files.length)
            return;

        exibirPreviewImagem(this, this.files[0]);
    });
}


function exibirPreviewImagem(input, arquivo) {

    const preview =
        document.getElementById("preview-foto-principal");

    if (!preview)
        return;

    const reader = new FileReader();

    reader.onload = function (e) {

        preview.innerHTML = `
            <div class="card border shadow-sm mt-2">
                <div class="card-header bg-light fw-semibold">
                    Pré-visualização
                </div>

                <div class="card-body text-center">
                    <img src="${e.target.result}" alt="Pré-visualização" class="img-fluid rounded" style="max-height: 220px; object-fit: cover;" />
                </div>
            </div>
        `;
    };

    reader.readAsDataURL(arquivo);
}