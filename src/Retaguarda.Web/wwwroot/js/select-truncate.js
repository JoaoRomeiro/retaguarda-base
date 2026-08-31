// Normalizador global de <select>: limita o texto visível das <option> e preserva o texto completo
// no atributo `title` (hover). Aplica-se a TODOS os selects da página no load — sem editar view por
// view — e expõe `RtSelect.applyTo` para selects populados via JS (ex.: AJAX). Vanilla, sem
// dependências (docs/padrao-ui.md §8).
//
// Por que JS e não CSS: a lista aberta de um <select> nativo é desenhada pelo navegador/SO; não há
// como, de forma confiável e cross-browser, limitar sua largura ou aplicar reticências via CSS.
// Truncar o texto da option é a correção determinística.
(function (window, document) {
    'use strict';

    // Limite padrão (caracteres) do texto visível. Ajustável por select via data-label-max.
    var DEFAULT_MAX = 60;

    // Trunca no fim, removendo espaço à direita e anexando reticências (…). Reserva 1 caractere para
    // o "…" (que ocupa 1 posição).
    function truncate(text, max) {
        if (text.length <= max) {
            return text;
        }
        return text.slice(0, max - 1).replace(/\s+$/, '') + '…';
    }

    // Limite efetivo do select: data-label-max (quando > 0) ou o padrão.
    function maxFor(select) {
        var attr = parseInt(select.getAttribute('data-label-max'), 10);
        return attr > 0 ? attr : DEFAULT_MAX;
    }

    // Normaliza as options de UM select. Idempotente: guarda o texto completo em data-full e usa-o
    // como origem em re-execucoes (ex.: recalculo apos popular via AJAX).
    function applyTo(select) {
        if (!select || !select.options) {
            return;
        }

        var max = maxFor(select);
        for (var i = 0; i < select.options.length; i++) {
            var option = select.options[i];
            var full = (typeof option.dataset.full !== 'undefined')
                ? option.dataset.full
                : option.textContent.trim();

            if (full.length <= max) {
                continue; // cabe — placeholders e rotulos curtos ficam intactos.
            }

            option.dataset.full = full;
            if (!option.title) {
                option.title = full; // texto completo no hover, sem perda de informacao.
            }

            var truncated = truncate(full, max);
            if (option.textContent !== truncated) {
                // So reescreve quando muda — evita o observer entrar em loop ao se observar.
                option.textContent = truncated;
            }
        }
    }

    // Aplica a todos os selects sob um no (default: documento inteiro).
    function applyAll(root) {
        var selects = (root || document).querySelectorAll('select');
        for (var i = 0; i < selects.length; i++) {
            applyTo(selects[i]);
        }
    }

    // API publica para selects populados dinamicamente (ex.: produtos por AJAX). Opcional: com o
    // observer abaixo, options inseridas depois ja sao normalizadas automaticamente.
    window.RtSelect = { applyTo: applyTo, applyAll: applyAll };

    // Debounce: coalesce rajadas de mutacao numa unica passada por frame.
    var scheduled = false;
    function scheduleApplyAll() {
        if (scheduled) {
            return;
        }
        scheduled = true;
        window.requestAnimationFrame(function () {
            scheduled = false;
            applyAll();
        });
    }

    // Observa insercoes no DOM e normaliza selects/options adicionados depois (AJAX, modais,
    // formularios dinamicos) — sem a tela precisar chamar nada. A reescrita de texto e guardada
    // (so muda quando difere), entao o observer nao entra em loop ao observar a si proprio.
    function startObserver() {
        if (!window.MutationObserver) {
            return;
        }
        var observer = new MutationObserver(function (mutations) {
            for (var i = 0; i < mutations.length; i++) {
                if (mutations[i].addedNodes.length > 0) {
                    scheduleApplyAll();
                    return;
                }
            }
        });
        observer.observe(document.body, { childList: true, subtree: true });
    }

    function init() {
        applyAll();
        startObserver();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})(window, document);
