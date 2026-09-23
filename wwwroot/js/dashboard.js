window.renderDashboardCharts = function(dadosStatus, dadosPrioridade, dadosPrazo, dadosResponsavel) {
    // 1. Gráfico de Tarefas por Status (Donut)
    const ctxStatus = document.getElementById("chartStatus");
    if (ctxStatus) {
        new Chart(ctxStatus, {
            type: "doughnut",
            data: {
                labels: Object.keys(dadosStatus),
                datasets: [{
                    data: Object.values(dadosStatus),
                    backgroundColor: ["#3b82f6", "#f59e0b", "#8b5cf6", "#10b981", "#64748b"],
                    borderWidth: 2,
                    borderColor: "#ffffff"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: "bottom" }
                }
            }
        });
    }

    // 2. Gráfico por Prioridade (Barra)
    const ctxPrio = document.getElementById("chartPrioridade");
    if (ctxPrio) {
        new Chart(ctxPrio, {
            type: "bar",
            data: {
                labels: Object.keys(dadosPrioridade),
                datasets: [{
                    label: "Tarefas",
                    data: Object.values(dadosPrioridade),
                    backgroundColor: ["#94a3b8", "#0284c7", "#f97316", "#ef4444"],
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, ticks: { stepSize: 1 } }
                }
            }
        });
    }

    // 3. Gráfico Cumprimento de Prazo (Pizza)
    const ctxPrazo = document.getElementById("chartPrazo");
    if (ctxPrazo) {
        new Chart(ctxPrazo, {
            type: "pie",
            data: {
                labels: Object.keys(dadosPrazo),
                datasets: [{
                    data: Object.values(dadosPrazo),
                    backgroundColor: ["#10b981", "#ef4444", "#94a3b8"],
                    borderWidth: 2,
                    borderColor: "#ffffff"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: "bottom" }
                }
            }
        });
    }

    // 4. Gráfico por Responsável (Barra Horizontal)
    const ctxResp = document.getElementById("chartResponsavel");
    if (ctxResp) {
        new Chart(ctxResp, {
            type: "bar",
            indexAxis: "y",
            data: {
                labels: Object.keys(dadosResponsavel),
                datasets: [{
                    label: "Tarefas Atribuídas",
                    data: Object.values(dadosResponsavel),
                    backgroundColor: "#0284c7",
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, ticks: { stepSize: 1 } }
                }
            }
        });
    }
};
