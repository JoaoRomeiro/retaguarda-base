// Helper de loading para formulários marcados com data-disable-on-submit.
// Convenção: ao submeter um form com esse atributo, os botões de submit
// recebem spinner + ficam disabled, prevenindo duplo-envio.
// Strings visíveis vêm de data-loading-text (preenchido via IStringLocalizer
// na view); sem fallback hardcoded.
// Ver docs/padrao-ui.md §7 (Loading states).
'use strict';
(function () {
    // Aplica estado de loading no botão; preserva HTML original para restauração.
    function setLoadingState(button) {
        if (!button.dataset.originalHtml) {
            button.dataset.originalHtml = button.innerHTML;
        }
        var spinner = '<span class="spinner-border spinner-border-sm me-2" '
                    + 'role="status" aria-hidden="true"></span>';
        // Sem data-loading-text? mantém o HTML original (zero hardcode no JS).
        var label = button.dataset.loadingText
            ? escapeHtml(button.dataset.loadingText)
            : button.dataset.originalHtml;
        button.innerHTML = spinner + label;
        button.disabled = true;
        button.setAttribute('aria-busy', 'true');
    }

    // Restaura botão ao estado pré-submit (usado quando volta via bfcache).
    function clearLoadingState(button) {
        if (button.dataset.originalHtml) {
            button.innerHTML = button.dataset.originalHtml;
            delete button.dataset.originalHtml;
        }
        button.disabled = false;
        button.removeAttribute('aria-busy');
    }

    // Escapa texto vindo de data-loading-text (defesa em profundidade contra XSS).
    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    // Listener delegado: dispara só após validação nativa do browser passar.
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (!form.hasAttribute('data-disable-on-submit')) return;
        // Outro handler cancelou o submit (ex.: Bootstrap was-validated).
        if (event.defaultPrevented) return;
        // Defesa contra submit programático (form.submit() pula validação).
        if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;

        var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
        buttons.forEach(setLoadingState);
    });

    // Submit automático para campos marcados com data-submit-on-change.
    // Útil para filtros por select/checkbox sem JS específico por tela.
    document.addEventListener('change', function (event) {
        var control = event.target;
        if (!(control instanceof HTMLElement)) return;
        if (!control.hasAttribute('data-submit-on-change')) return;

        var form = control.closest('form');
        if (!(form instanceof HTMLFormElement)) return;

        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
            return;
        }

        form.submit();
    });

    // bfcache: ao voltar via back/forward, o DOM é restaurado mas o estado
    // disabled do botão fica preso. event.persisted=true sinaliza esse caso.
    window.addEventListener('pageshow', function (event) {
        if (!event.persisted) return;
        var stuck = document.querySelectorAll(
            'form[data-disable-on-submit] [aria-busy="true"]'
        );
        stuck.forEach(clearLoadingState);
    });
})();
