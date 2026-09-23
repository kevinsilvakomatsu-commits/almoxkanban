document.addEventListener("DOMContentLoaded", () => {
    const columns = document.querySelectorAll(".kanban-cards");
    if (!columns.length) return;

    columns.forEach(col => {
        new Sortable(col, {
            group: "kanban-tasks",
            animation: 180,
            ghostClass: "sortable-ghost",
            chosenClass: "sortable-chosen",
            dragClass: "sortable-drag",
            onEnd: async function (evt) {
                const itemEl = evt.item;
                const targetCol = evt.to;
                const sourceCol = evt.from;
                const tarefaId = itemEl.getAttribute("data-id");
                const novoStatus = targetCol.getAttribute("data-status");

                if (targetCol === sourceCol) return;

                // Se moveu para "Concluída", verificar se precisa do fluxo com fotos
                if (novoStatus === "Concluida") {
                    // Reverter temporariamente a posição no DOM
                    sourceCol.insertBefore(itemEl, sourceCol.children[evt.oldIndex]);
                    
                    window.confirmarAcao(
                        "Concluir Tarefa com Evidência",
                        "Para concluir uma tarefa é obrigatório registrar a foto da resolução e uma breve descrição. Deseja ir para a tela de conclusão agora?",
                        () => {
                            window.location.href = `/Tarefas/Concluir?id=${tarefaId}`;
                        }
                    );
                    return;
                }

                // Chamar backend para persistir movimentação
                try {
                    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
                    const response = await fetch("/Index?handler=MoverStatus", {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "RequestVerificationToken": token,
                            "XSRF-TOKEN": token
                        },
                        body: JSON.stringify({
                            tarefaId: parseInt(tarefaId),
                            novoStatus: novoStatus
                        })
                    });

                    if (!response.ok) {
                        const erroTexto = await response.text();
                        console.error("Erro HTTP ao mover tarefa:", response.status, erroTexto);
                        sourceCol.insertBefore(itemEl, sourceCol.children[evt.oldIndex]);
                        window.showToast("erro", "Erro ao atualizar status (HTTP " + response.status + ").");
                        return;
                    }

                    const data = await response.json();
                    if (data.sucesso) {
                        window.showToast("sucesso", data.mensagem || "Status atualizado com sucesso!");
                        atualizarContadores();
                    } else {
                        // Reverter posição em caso de erro
                        sourceCol.insertBefore(itemEl, sourceCol.children[evt.oldIndex]);
                        window.showToast("erro", data.erro || "Não foi possível mover a tarefa.");
                    }
                } catch (err) {
                    console.error("Exceção ao atualizar status:", err);
                    sourceCol.insertBefore(itemEl, sourceCol.children[evt.oldIndex]);
                    window.showToast("erro", "Erro de conexão ao atualizar status.");
                }
            }
        });
    });

    function atualizarContadores() {
        document.querySelectorAll(".kanban-column").forEach(col => {
            const countBadge = col.querySelector(".badge-count");
            const cards = col.querySelectorAll(".kanban-card");
            if (countBadge) countBadge.innerText = cards.length;
        });
    }
});
