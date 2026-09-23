document.addEventListener("DOMContentLoaded", () => {
    const kanbanBoard = document.getElementById("kanbanBoard");
    const columns = document.querySelectorAll(".kanban-cards");
    if (!columns.length) return;

    // Inicialização do Drag & Drop com SortableJS
    columns.forEach(col => {
        new Sortable(col, {
            group: "kanban-tasks",
            animation: 180,
            delay: 150,
            delayOnTouchOnly: true,
            touchStartThreshold: 5,
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

                // Se moveu para "Concluída", redirecionar para comprovação com fotos
                if (novoStatus === "Concluida") {
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
                        atualizarMenuCard(itemEl, novoStatus);
                    } else {
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

    // Ação Rápida de Mover Card (para Mobile e Acessibilidade)
    document.addEventListener("click", async function (e) {
        const btn = e.target.closest(".btn-quick-move");
        if (!btn) return;

        e.preventDefault();
        const tarefaId = btn.getAttribute("data-id");
        const novoStatus = btn.getAttribute("data-target");
        const card = document.querySelector(`.kanban-card[data-id="${tarefaId}"]`);
        if (!card) return;

        // Se mover para Concluída
        if (novoStatus === "Concluida") {
            window.location.href = `/Tarefas/Concluir?id=${tarefaId}`;
            return;
        }

        const targetColumnCards = document.querySelector(`.kanban-cards[data-status="${novoStatus}"]`);
        if (!targetColumnCards) return;

        btn.disabled = true;

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
                window.showToast("erro", "Erro ao atualizar status.");
                btn.disabled = false;
                return;
            }

            const data = await response.json();
            if (data.sucesso) {
                // Move o elemento visualmente no DOM com animação suave
                card.style.transition = "opacity 0.2s ease, transform 0.2s ease";
                card.style.opacity = "0.4";
                card.style.transform = "scale(0.96)";

                setTimeout(() => {
                    targetColumnCards.prepend(card);
                    card.style.opacity = "1";
                    card.style.transform = "none";
                    atualizarContadores();
                    atualizarMenuCard(card, novoStatus);
                    window.showToast("sucesso", `Tarefa movida para ${novoStatus}!`);
                }, 200);
            } else {
                window.showToast("erro", data.erro || "Não foi possível mover.");
            }
        } catch (err) {
            console.error("Erro ao mover via ação rápida:", err);
            window.showToast("erro", "Erro de comunicação ao mover tarefa.");
        } finally {
            btn.disabled = false;
        }
    });

    // Atualiza opções do menu dropdown de um card movido
    function atualizarMenuCard(card, statusAtual) {
        const id = card.getAttribute("data-id");
        const dropdownMenu = card.querySelector(".dropdown-menu");
        if (!dropdownMenu) return;

        let html = `<li><h6 class="dropdown-header py-1 text-muted small"><i class="bi bi-arrow-left-right me-1"></i> Mover Tarefa</h6></li>`;
        
        if (statusAtual !== "Pendente") {
            html += `<li><button type="button" class="dropdown-item py-1 small btn-quick-move" data-id="${id}" data-target="Pendente"><i class="bi bi-circle-fill text-primary me-2" style="font-size: 0.6rem;"></i>Pendente</button></li>`;
        }
        if (statusAtual !== "EmAndamento") {
            html += `<li><button type="button" class="dropdown-item py-1 small btn-quick-move" data-id="${id}" data-target="EmAndamento"><i class="bi bi-arrow-repeat text-warning me-2"></i>Em Andamento</button></li>`;
        }
        if (statusAtual !== "Aguardando") {
            html += `<li><button type="button" class="dropdown-item py-1 small btn-quick-move" data-id="${id}" data-target="Aguardando"><i class="bi bi-pause-circle-fill me-2" style="color: #8b5cf6;"></i>Aguardando</button></li>`;
        }
        if (statusAtual !== "Concluida") {
            html += `<li><hr class="dropdown-divider my-1"></li><li><a class="dropdown-item py-1 small text-success fw-semibold" href="/Tarefas/Concluir?id=${id}"><i class="bi bi-check2-circle me-2"></i>Concluir com Foto</a></li>`;
        }
        html += `<li><hr class="dropdown-divider my-1"></li><li><a class="dropdown-item py-1 small text-secondary" href="/Tarefas/Detalhes?id=${id}"><i class="bi bi-eye me-2"></i>Ver Detalhes</a></li>`;
        
        dropdownMenu.innerHTML = html;
    }

    // Atualização de contadores em colunas e abas móveis
    function atualizarContadores() {
        let totalPendente = 0, totalAndamento = 0, totalAguardando = 0, totalConcluida = 0;

        document.querySelectorAll(".kanban-column").forEach(col => {
            const countBadge = col.querySelector(".badge-count");
            const cards = col.querySelectorAll(".kanban-card");
            const count = cards.length;
            if (countBadge) countBadge.innerText = count;

            if (col.id === "col-pendente") totalPendente = count;
            else if (col.id === "col-emandamento") totalAndamento = count;
            else if (col.id === "col-aguardando") totalAguardando = count;
            else if (col.id === "col-concluida") totalConcluida = count;
        });

        const tabP = document.getElementById("tabCountPendente");
        const tabE = document.getElementById("tabCountEmAndamento");
        const tabA = document.getElementById("tabCountAguardando");
        const tabC = document.getElementById("tabCountConcluida");

        if (tabP) tabP.innerText = totalPendente;
        if (tabE) tabE.innerText = totalAndamento;
        if (tabA) tabA.innerText = totalAguardando;
        if (tabC) tabC.innerText = totalConcluida;
    }

    // Gerenciador de Abas de Colunas no Mobile
    const tabButtons = document.querySelectorAll(".kanban-tab-btn");
    tabButtons.forEach(btn => {
        btn.addEventListener("click", () => {
            const targetId = btn.getAttribute("data-col-target");
            const targetEl = document.getElementById(targetId);
            if (!targetEl) return;

            tabButtons.forEach(b => b.classList.remove("active"));
            btn.classList.add("active");

            targetEl.scrollIntoView({
                behavior: "smooth",
                block: "nearest",
                inline: "center"
            });
        });
    });

    // Sincroniza aba ativa conforme o usuário desliza horizontalmente as colunas no celular
    if (kanbanBoard && window.innerWidth <= 768) {
        let scrollTimeout;
        kanbanBoard.addEventListener("scroll", () => {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                const boardRect = kanbanBoard.getBoundingClientRect();
                const boardCenter = boardRect.left + boardRect.width / 2;

                let colMaisProxima = null;
                let menorDistancia = Infinity;

                document.querySelectorAll(".kanban-column").forEach(col => {
                    const rect = col.getBoundingClientRect();
                    const colCenter = rect.left + rect.width / 2;
                    const distancia = Math.abs(colCenter - boardCenter);
                    if (distancia < menorDistancia) {
                        menorDistancia = distancia;
                        colMaisProxima = col;
                    }
                });

                if (colMaisProxima) {
                    const targetId = colMaisProxima.id;
                    tabButtons.forEach(b => {
                        b.classList.toggle("active", b.getAttribute("data-col-target") === targetId);
                    });
                }
            }, 80);
        }, { passive: true });
    }
});
