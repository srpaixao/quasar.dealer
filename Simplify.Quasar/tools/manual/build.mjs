import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { marked } from 'marked';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(scriptDirectory, '..', '..');
const docsRoot = path.resolve(projectRoot, '..', 'docs');
const outputRoot = path.join(projectRoot, 'App_Data', 'Manual');
const pagesOutput = path.join(outputRoot, 'pages');
const assetsOutput = path.join(outputRoot, 'assets');

const pages = [
  { slug: 'inicio', source: 'MANUAL_UTILIZACAO.md' },
  { slug: 'recebimento', source: 'MANUAL_TELAS_RECEBIMENTO.md' },
  { slug: 'estoque', source: 'MANUAL_TELAS_ESTOQUE.md' },
  { slug: 'separacao', source: 'MANUAL_TELAS_SEPARACAO.md' },
  { slug: 'expedicao', source: 'MANUAL_TELAS_EXPEDICAO.md' },
  { slug: 'devolucao', source: 'MANUAL_TELAS_DEVOLUCAO.md' },
  { slug: 'anomalias', source: 'MANUAL_TELAS_ANOMALIAS.md' },
  { slug: 'cadastros', source: 'MANUAL_TELAS_CADASTROS.md' }
];

const pageBySource = new Map(pages.map(page => [page.source.toLowerCase(), page.slug]));
const assetFolders = [
  'manual-recebimento',
  'manual-estoque',
  'manual-separacao',
  'manual-expedicao'
];

function slugify(value) {
  return value
    .replace(/<[^>]+>/g, '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '') || 'secao';
}

function addHeadingIds(html) {
  const occurrences = new Map();
  return html.replace(/<h([1-6])>([\s\S]*?)<\/h\1>/gi, (_, level, content) => {
    const base = slugify(content);
    const count = occurrences.get(base) || 0;
    occurrences.set(base, count + 1);
    const id = count === 0 ? base : `${base}-${count + 1}`;
    return `<h${level} id="${id}">${content}</h${level}>`;
  });
}

function rewriteLinks(html) {
  let result = html.replace(/href="([^"#?]+\.md)(#[^"]*)?"/gi, (match, target, anchor = '') => {
    const fileName = path.basename(target).toLowerCase();
    const slug = pageBySource.get(fileName);
    return slug
      ? `href="__MANUAL_PAGE__${slug}${anchor}"`
      : 'class="manual-unavailable" title="Conteúdo técnico não publicado no manual operacional"';
  });

  result = result.replace(
    /src="assets\/([^"]+)"/gi,
    'src="__MANUAL_ASSET__$1" loading="lazy"'
  );

  return result;
}

function removeUnsafeHtml(html) {
  return html
    .replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, '')
    .replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, '')
    .replace(/\son[a-z]+\s*=\s*"[^"]*"/gi, '')
    .replace(/\son[a-z]+\s*=\s*'[^']*'/gi, '');
}

function buildPage(page) {
  const sourcePath = path.join(docsRoot, page.source);
  if (!fs.existsSync(sourcePath)) {
    throw new Error(`Documento não localizado: ${sourcePath}`);
  }

  let markdown = fs.readFileSync(sourcePath, 'utf8');
  if (page.slug === 'inicio') {
    markdown = markdown.replace(/\n## Referências[\s\S]*$/i, '');
  }
  let html = marked.parse(markdown, {
    gfm: true,
    breaks: false
  });

  html = removeUnsafeHtml(html);
  html = addHeadingIds(html);
  html = rewriteLinks(html);
  fs.writeFileSync(path.join(pagesOutput, `${page.slug}.html`), html, 'utf8');
}

function copyAssets() {
  for (const folder of assetFolders) {
    const source = path.join(docsRoot, 'assets', folder);
    const destination = path.join(assetsOutput, folder);
    if (!fs.existsSync(source)) {
      continue;
    }

    fs.cpSync(source, destination, {
      recursive: true,
      force: true,
      filter: item => !item.toLowerCase().endsWith('readme.md')
    });
  }
}

fs.mkdirSync(pagesOutput, { recursive: true });
fs.rmSync(assetsOutput, { recursive: true, force: true });
fs.mkdirSync(assetsOutput, { recursive: true });

for (const page of pages) {
  buildPage(page);
}
copyAssets();

console.log(`Manual gerado em ${outputRoot}`);
