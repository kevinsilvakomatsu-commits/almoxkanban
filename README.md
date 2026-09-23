# AlmoxKanban — Sistema de Gestão de Tarefas do Almoxarifado

Aplicação web local completa desenvolvida para a gestão e acompanhamento operacional das tarefas da equipe do almoxarifado, utilizando um quadro visual Kanban, controle de evidências fotográficas, dashboard com indicadores e relatórios em Excel.

---

## 🚀 Como Executar o Sistema (Passo a Passo)

### Opção 1: Inicialização em 1 Clique (Mais Fácil)
1. Navegue até a pasta do projeto:
   `\\forest.local\dfs\Brazil\Trabalho\Filiais\BUTIÁ\05 - PROJETOS (KEV)\TAREFAS\AlmoxKanban` (ou pela unidade `W:\...`)
2. Dê um duplo clique no arquivo **`Iniciar_AlmoxKanban.bat`**.
3. O servidor será iniciado e o navegador abrirá automaticamente na tela de login.

---

### Opção 2: Pelo Terminal / PowerShell
1. Abra o PowerShell ou Prompt de Comando.
2. Acesse a pasta do projeto:
   ```powershell
   cd "W:\Brazil\Trabalho\Filiais\BUTIÁ\05 - PROJETOS (KEV)\TAREFAS\AlmoxKanban"
   ```
3. Execute o comando:
   ```powershell
   dotnet run
   ```
4. Abra seu navegador de internet e acesse:
   **`http://localhost:5000`**

---

## 🔑 Credenciais do Primeiro Administrador

Ao executar pela primeira vez, o sistema cria automaticamente o banco de dados e o usuário administrador principal:

- **Usuário:** `admin`
- **Senha Inicial:** `Admin@123`
- **Regra de Segurança:** No primeiro acesso, o sistema exigirá obrigatoriamente a troca da senha inicial por uma nova senha pessoal de sua preferência.

---

## 🌐 Como Acessar de Outros Computadores da Rede Local

O sistema está configurado para atender a rede interna (`http://0.0.0.0:5000`).

1. No computador principal onde o sistema está rodando, abra o PowerShell e digite:
   ```powershell
   ipconfig
   ```
2. Localize o **Endereço IPv4** (exemplo: `192.168.1.50` ou o IP corporativo).
3. Nos outros computadores da rede, basta abrir o navegador e digitar:
   **`http://192.168.1.50:5000`** (substituindo pelo IP do computador principal).
4. Cada membro da equipe pode acessar com seu próprio usuário e senha.

---

## 📋 Funcionalidades do Sistema

### 1. Quadro Kanban
- **4 Colunas:** Pendente, Em Andamento, Aguardando e Concluída.
- **Arrastar e Soltar (Drag and Drop):** Movimente os cartões entre as colunas com atualização imediata no banco de dados.
- **Cartões com Foto Principal em Destaque:** Exibe imagem nítida, título, breve descrição, avatares dos responsáveis, prioridade (Baixa, Normal, Alta, Urgente), prazo e alertas visuais de atraso.
- **Barra de Filtros:** Filtragem por texto (título/descrição), responsável, prioridade, tarefas atrasadas, tarefas com prazo para hoje, sem prazo ou "Minhas Tarefas".

### 2. Conclusão de Tarefa com Evidência Fotográfica
- Para concluir uma tarefa, é **obrigatório** anexar pelo menos uma foto comprovando o serviço executado e uma breve descrição da solução.
- O sistema registra automaticamente a data, hora exata e o usuário responsável pela conclusão.

### 3. Histórico e Linha do Tempo
- Toda tarefa possui uma linha do tempo completa registrando: criação, edição, movimentações entre status, novos comentários, anexos e conclusões.
- Administradores podem **reabrir tarefas concluídas** mediante justificativa registrada.

### 4. Dashboard Operacional
- Indicadores reais: Total de tarefas, pendentes, em andamento, concluídas, atrasadas, percentual de conclusão e tempo médio de resolução.
- 4 Gráficos interativos (Chart.js local):
  - Tarefas por Status
  - Tarefas por Prioridade
  - Cumprimento de Prazo (Dentro do Prazo vs Fora do Prazo)
  - Volume de Tarefas por Responsável
- Filtros por período, colaborador, prioridade e status.

### 5. Gestão de Equipe
- Cadastro e edição de integrantes da equipe com foto, função e permissões (Líder/Admin ou Usuário Operacional).
- Contadores operacionais individuais (tarefas pendentes, em andamento e concluídas).
- Importação em lote de colaboradores através de planilha Excel.

### 6. Backup e Restauração dos Dados
- **Geração de Cópia em 1 Clique:** Na aba "Backup & Dados", clique em **"Baixar Arquivo de Backup Agora"**.
- O sistema gera um arquivo `.zip` contendo:
  - O arquivo SQLite `Data/tarefas.db`
  - Todas as fotos armazenadas (`wwwroot/uploads`)
- **Para restaurar um backup:** basta extrair o arquivo `.zip` e substituir o arquivo `Data/tarefas.db` e o conteúdo de `wwwroot/uploads`.

### 7. Exportação e Importação por Excel
- **Exportar Tarefas:** Gera planilha `.xlsx` com todas as tarefas, status, responsáveis, prazos e avaliação de cumprimento de prazos.
- **Planilha Modelo:** Disponível na tela da Equipe ou na pasta `wwwroot/templates/modelo_importacao.xlsx`.
- **Independente:** Não requer o aplicativo Microsoft Excel instalado no servidor.

---

## 📁 Estrutura de Pastas do Projeto

```
AlmoxKanban/
├── Iniciar_AlmoxKanban.bat   # Inicializador automático em 1 clique
├── Program.cs                # Configurações do servidor, injeção e segurança
├── appsettings.json          # Configuração da conexão com SQLite
├── Data/
│   ├── AppDbContext.cs       # Contexto do Entity Framework Core
│   ├── SeedData.cs           # Criação do usuário admin inicial
│   └── tarefas.db            # Banco de dados local SQLite
├── Models/                   # 11 Entidades e Enums do sistema
├── Services/                 # Regras de negócio, uploads, auditoria, Excel e backups
├── Pages/                    # Páginas Razor (Kanban, Tarefas, Dashboard, Equipe, Backup)
├── wwwroot/
│   ├── css/site.css          # Design visual moderno para almoxarifado
│   ├── js/                   # Scripts locais (Kanban drag-and-drop, gráficos)
│   ├── lib/                  # Bibliotecas locais sem CDN (Bootstrap, Icons, SortableJS, Chart.js)
│   ├── uploads/              # Armazenamento físico de imagens organizadas por pastas
│   └── templates/            # Planilha modelo para importação
└── README.md                 # Este manual completo
```

---

## 🔒 Segurança e Privacidade
- 100% autônomo e sem conexão externa ou dependência de internet.
- Senhas protegidas com criptografia hash BCrypt.
- Proteção contra ataques CSRF em todos os formulários.
- Uploads validados por extensão e com identificadores GUID únicos para evitar conflitos de nomes.
