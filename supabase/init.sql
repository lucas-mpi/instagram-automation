-- =============================================================================
-- Automação de Conteúdo para Estética - Supabase Setup (Database + Seed 30 Temas)
-- =============================================================================

-- 1. Criar tabela de pautas / temas
CREATE TABLE IF NOT EXISTS public.temas (
    id SERIAL PRIMARY KEY,
    servico TEXT NOT NULL CHECK (servico IN ('cilios', 'maquiagem', 'escova', 'babyliss')),
    formato TEXT NOT NULL,
    tema TEXT NOT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    ultimo_uso TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Índice para acelerar a busca do tema ativo mais antigo
CREATE INDEX IF NOT EXISTS idx_temas_ultimo_uso ON public.temas (ativo, ultimo_uso ASC NULLS FIRST, id ASC);

-- 2. Inserir os 30 temas específicos (sementes) do documento
INSERT INTO public.temas (id, servico, formato, tema, ultimo_uso) VALUES
-- Extensão de Cílios (10 temas)
(1, 'cilios', 'cuidados_pos', 'Higienização diária: Por que lavar os cílios todos os dias com shampoo neutro infantil/espuma evita blefarite e aumenta a retenção.', NULL),
(2, 'cilios', 'mito_ou_verdade', 'Demaquilante bifásico: O motivo químico pelo qual óleos dissolvem a cola (cianoacrilato) e derrubam os fios antes da hora.', NULL),
(3, 'cilios', 'mito_ou_verdade', 'Ciclo natural dos fios: Explicação rápida de que perder de 2 a 5 fios naturais por dia é normal e não significa que a extensão estragou o cílio.', NULL),
(4, 'cilios', 'mito_ou_verdade', 'Regra das 24h vs. realidade atual: Como funcionam os adesivos modernos e o que realmente evitar nas primeiras horas (vapor intenso/sauna).', NULL),
(5, 'cilios', 'cuidados_pos', 'Escovar os cílios: Como usar a escovinha corretamente (apenas do meio para as pontas, com os fios secos) para não entortar o mapping.', NULL),
(6, 'cilios', 'checklist_preparacao', 'Preparação pré-atendimento: Por que ir sem máscara de cílios e evitar café/energéticos 2h antes melhora a precisão e a colagem.', NULL),
(7, 'cilios', 'guia_de_escolha', 'Fio a Fio vs. Volume Brasileiro: Comparativo prático para ajudar quem busca naturalidade vs. quem quer efeito de rímel marcado.', NULL),
(8, 'cilios', 'cuidados_pos', 'Manutenção (15 a 21 dias): Por que deixar passar de 25 dias acaba exigindo uma nova aplicação completa em vez de manutenção.', NULL),
(9, 'cilios', 'cuidados_pos', 'Posição ao dormir: Dicas práticas para quem dorme de bruços ou de lado não amassar apenas um dos olhos.', NULL),
(10, 'cilios', 'mito_ou_verdade', 'Rímel na extensão: Por que nunca usar máscara de cílios (especialmente à prova d''água) por cima dos fans/fios sintéticos.', NULL),

-- Maquiagem Social e Eventos (8 temas)
(11, 'maquiagem', 'checklist_preparacao', 'Skincare pré-make: Como a hidratação da pele nos 3 dias anteriores ao evento impede que a base craquele ou marque linhas.', NULL),
(12, 'maquiagem', 'guia_de_escolha', 'Make de dia vs. Make de noite: Diferenças de iluminação, intensidade de contorno e fixação para casamento ao ar livre vs. festa noturna.', NULL),
(13, 'maquiagem', 'mito_ou_verdade', 'Pele madura e textura: Por que preparar a pele com produtos hidratantes e pontos de luz estratégicos valoriza muito mais do que excesso de pó.', NULL),
(14, 'maquiagem', 'cuidados_pos', 'Durabilidade em dias quentes/úmidos: Técnicas de blindagem e selagem que mantêm a maquiagem intacta mesmo com suor ou lágrimas.', NULL),
(15, 'maquiagem', 'guia_de_escolha', 'Como escolher a referência certa: Dicas para a cliente buscar fotos de inspiração com formato de olho e tom de pele parecidos com os dela.', NULL),
(16, 'maquiagem', 'checklist_preparacao', 'Cuidados com os lábios: Esfoliação leve e balm antes do atendimento para o batom matte não ressecar durante a festa.', NULL),
(17, 'maquiagem', 'guia_de_escolha', 'Maquiagem + Extensão de Cílios: Como adaptar o delineado e a sombra quando a cliente já usa extensão de cílios.', NULL),
(18, 'maquiagem', 'cuidados_pos', 'Remoção correta pós-festa: O passo a passo para limpar a pele sem agredir a barreira cutânea (e sem arrancar os cílios postiços ou de extensão).', NULL),

-- Escova (6 temas)
(19, 'escova', 'mito_ou_verdade', 'Protetor térmico obrigatório: O que acontece com a cutícula do fio exposta a 200 °C sem proteção e como o produto sela o brilho.', NULL),
(20, 'escova', 'guia_de_escolha', 'Escova modelada vs. Escova lisa: Quando escolher pontas viradas/movimento natural e quando apostar no alinhamento total.', NULL),
(21, 'escova', 'cuidados_pos', 'Combate ao frizz na umidade: Como blindar o cabelo escovado contra a umidade do ar usando finalizadores leves (sem pesar).', NULL),
(22, 'escova', 'cuidados_pos', 'Como dormir sem perder a escova: Técnica do coque frouxo no topo da cabeça ("abacaxi") ou fronha de cetim para acordar com o cabelo alinhado.', NULL),
(23, 'escova', 'checklist_preparacao', 'Pré-lavagem antes do salão: O que evitar passar em casa antes de ir escovar (excesso de óleo reparador ou leave-in pesado deixa o fio oleoso).', NULL),
(24, 'escova', 'checklist_preparacao', 'Escova para eventos: Por que fazer a escova algumas horas antes garante que o cabelo assente com movimento natural até a hora da festa.', NULL),

-- Babyliss e Ondas (6 temas)
(25, 'babyliss', 'guia_de_escolha', 'Cachos clássicos vs. Beach Waves: Diferença visual entre ondas marcadas e o efeito despojado/moderno com pontas retas.', NULL),
(26, 'babyliss', 'mito_ou_verdade', 'Por que o babyliss desmancha rápido: Os 3 principais culpados (excesso de condicionador/máscara na lavagem, falta de fixador ou cabelo muito fino sem textura).', NULL),
(27, 'babyliss', 'cuidados_pos', 'O segredo do resfriamento do cacho: Por que não se deve pentear ou passar a mão nas ondas enquanto o cabelo ainda está quente.', NULL),
(28, 'babyliss', 'guia_de_escolha', 'Cabelo curto/médio com babyliss: Como ondas largas trazem volume e sofisticação mesmo para cortes chanel ou long bob.', NULL),
(29, 'babyliss', 'cuidados_pos', 'Como soltar os cachos no dia seguinte: Como transformar ondas mais fechadas da festa em um ondulado natural para o dia seguinte sem usar calor de novo.', NULL),
(30, 'babyliss', 'guia_de_escolha', 'Combo Escova + Babyliss: Por que escovar a raiz antes de fazer o babyliss garante acabamento de salão e evita aspecto armado.', NULL)
ON CONFLICT (id) DO UPDATE SET
    servico = EXCLUDED.servico,
    formato = EXCLUDED.formato,
    tema = EXCLUDED.tema;

-- Ajustar sequence do ID
SELECT setval(pg_get_serial_sequence('public.temas', 'id'), (SELECT MAX(id) FROM public.temas));

-- 3. Criar bucket público "stories" no Supabase Storage (para hospedar o PNG temporariamente)
INSERT INTO storage.buckets (id, name, public)
VALUES ('stories', 'stories', true)
ON CONFLICT (id) DO NOTHING;

-- 4. Configurar segurança (RLS ativo: bloqueia acesso público/anon na tabela e uploads; apenas service_role / Secret Key acessa)
ALTER TABLE public.temas ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON TABLE public.temas FROM anon, authenticated;
GRANT ALL ON TABLE public.temas TO service_role;
GRANT USAGE, SELECT ON SEQUENCE public.temas_id_seq TO service_role;

-- No Storage, permitir apenas leitura pública no bucket 'stories' (para a Meta baixar o PNG do Story)
DROP POLICY IF EXISTS "Permitir upload de stories" ON storage.objects;
DROP POLICY IF EXISTS "Permitir update de stories" ON storage.objects;
DROP POLICY IF EXISTS "Permitir leitura publica de stories" ON storage.objects;

CREATE POLICY "Permitir leitura publica de stories"
ON storage.objects FOR SELECT
USING (bucket_id = 'stories');


