# Automação de Conteúdo para Estética (Instagram Stories Bot)

Pipeline 100% gratuito em **.NET 8**, **Google Gemini API (Structured Outputs)**, **Playwright**, **Supabase** e **Instagram Graph API** para gerar e publicar automaticamente cards tipográficos minimalistas nos Stories (1080x1920px) a cada 2 dias via **GitHub Actions**.

---

## Arquitetura do Projeto

```
[GitHub Actions Cron (48h)]
        │
        ▼
[1. Supabase DB] ──► Seleciona o tema com `ultimo_uso` mais antigo (alternando o serviço)
        │
        ▼
[2. Google Gemini API] ──► Gera o roteiro JSON estrito com limites rígidos de caracteres
        │
        ▼
[3. Playwright (.NET)] ──► Injeta o JSON no template HTML/CSS (com variação de paleta) e gera o PNG 1080x1920
        │
        ▼
[4. Supabase Storage + Instagram Graph API] ──► Hospeda o PNG e publica o Story na conta profissional
```

---

## Estrutura de Pastas

- `src/InstagramStoryBot/`
  - `Program.cs`: Orquestrador dos 4 passos da automação.
  - `Configuration/BotOptions.cs`: Carregamento de variáveis de ambiente, `.env` e argumentos de CLI (`--dry-run`, `--tema-id`).
  - `Models/TemaSeed.cs` & `StoryContent.cs`: Contratos de dados e validação rígida de tamanho de caracteres.
  - `Services/SupabaseService.cs`: Seleção inteligente com rotação de serviço (`cilios`, `maquiagem`, `escova`, `babyliss`), upload para o bucket e atualização de `ultimo_uso`.
  - `Services/GeminiService.cs`: Integração com `gemini-2.5-flash` usando `responseSchema` (Structured Outputs) e diretrizes anti-clichê.
  - `Services/StoryRendererService.cs`: Renderização headless via Playwright com 3 variações de paleta CSS por serviço.
  - `Services/InstagramService.cs`: Publicação em 2 etapas na Instagram Graph API (`/media` + `/media_publish`).
  - `Templates/story-template.html`: Template 1080x1920px respeitando as zonas de segurança do Instagram Stories.
  - `Data/temas-seed.json`: Banco local com os 30 temas para testes offline/locais.
- `supabase/init.sql`: Script SQL completo para criar a tabela `temas`, inserir os 30 temas e criar o bucket `stories`.
- `.github/workflows/post-instagram.yml`: Workflow agendado (`cron`) e manual (`workflow_dispatch`).

---

## Como Configurar e Rodar

### 1. Configurar o Supabase
1. Crie um projeto gratuito no [Supabase](https://supabase.com/).
2. Abra o **SQL Editor** no painel do Supabase, cole o conteúdo de [`supabase/init.sql`](./supabase/init.sql) e execute.
3. Copie a `SUPABASE_URL` e a `SUPABASE_KEY` (`service_role` key recomendada para o worker poder gravar no Storage e atualizar a tabela sem bloqueio de RLS).

### 2. Testar Localmente
1. Copie o arquivo `.env.example` para `.env` na raiz do projeto e preencha suas chaves (ou rode sem chaves em modo `--dry-run` para testar a renderização visual com o banco local):
   ```powershell
   Copy-Item .env.example .env
   ```
2. Compile o projeto e instale o navegador Chromium do Playwright:
   ```powershell
   dotnet build src/InstagramStoryBot/InstagramStoryBot.csproj
   pwsh src/InstagramStoryBot/bin/Debug/net8.0/playwright.ps1 install chromium
   ```
3. Execute em modo de teste (`--dry-run`) ou forçando um tema específico (`--tema-id 9`):
   ```powershell
   dotnet run --project src/InstagramStoryBot/InstagramStoryBot.csproj -- --dry-run --tema-id 9
   ```
   A imagem PNG 1080x1920px e o HTML correspondente serão gerados na pasta `output/`.

### 3. Configurar o GitHub Actions (100% Gratuito)
No seu repositório do GitHub, acesse **Settings → Secrets and variables → Actions** e adicione os seguintes *Repository secrets*:

| Secret | Descrição |
| :--- | :--- |
| `GEMINI_API_KEY` | Chave da API do Google AI Studio (Gemini) |
| `SUPABASE_URL` | URL do seu projeto Supabase (`https://xyz.supabase.co`) |
| `SUPABASE_KEY` | Chave da API do Supabase (`service_role`) |
| `INSTAGRAM_ACCESS_TOKEN` | Token de longa duração da Instagram Graph API |
| `INSTAGRAM_USER_ID` | ID da conta profissional do Instagram |
| `INSTAGRAM_HANDLE` | `@` do perfil para exibir no rodapé do card (ex: `@studio.lash`) |
