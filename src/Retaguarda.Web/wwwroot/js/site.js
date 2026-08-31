'use strict';

// ============================================================
// Sidebar mobile — abre/fecha sem depender do offcanvas do
// Bootstrap (que zera o fundo escuro no breakpoint >=md).
// Desktop (>=md) a sidebar é fixa por CSS; este script só atua
// no mobile, deslizando-a via classe .is-open + backdrop.
// ============================================================
// ============================================================
// Sidebar recolhível (desktop) — alterna o modo "só ícones" e
// persiste a escolha em cookie. O cookie é lido server-side no
// _Layout para aplicar a classe já no render (sem flash; o CSP
// estrito não permite script inline anti-flash).
// ============================================================
(function () {
    var collapseBtn = document.getElementById('sidebarCollapse');
    if (!collapseBtn) {
        return;
    }
    var root = document.documentElement;

    function syncAria() {
        collapseBtn.setAttribute(
            'aria-expanded',
            root.classList.contains('sidebar-collapsed') ? 'false' : 'true');
    }
    syncAria();

    collapseBtn.addEventListener('click', function () {
        var collapsed = root.classList.toggle('sidebar-collapsed');
        // Persiste por 1 ano; lido no _Layout para evitar flash no próximo render.
        document.cookie = 'sidebar_collapsed=' + (collapsed ? '1' : '0') +
            '; path=/; max-age=31536000; SameSite=Lax';
        syncAria();
    });
})();

// ============================================================
// Recolher antes de navegar. Com a sidebar recolhida, o menu só
// está expandido porque o cursor está sobre ele (:hover). Ao
// clicar num link, a página recarrega e renderiza recolhida —
// mas o navegador não reavalia :hover com o cursor parado, então
// o menu fica recolhido após o reload, com um "flash" feio na
// transição. Solução: interceptar o clique, recolher o menu
// (.is-collapsing desliga a expansão via CSS) e só então navegar,
// deixando o recolhimento visível e o estado consistente com a
// próxima página. Atua só no desktop.
// ============================================================
(function () {
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar) {
        return;
    }
    var root = document.documentElement;
    var desktop = window.matchMedia('(min-width: 768px)');

    sidebar.addEventListener('click', function (event) {
        // Só age no desktop e com a sidebar recolhida (aberta por hover).
        if (!desktop.matches || !root.classList.contains('sidebar-collapsed')) {
            return;
        }
        var link = event.target.closest('a');
        if (!link) {
            return;
        }
        // Preserva abrir-em-nova-aba, modificadores e alvos externos/âncoras.
        if (event.button !== 0 || event.ctrlKey || event.metaKey ||
            event.shiftKey || event.altKey || link.target === '_blank') {
            return;
        }
        var rawHref = link.getAttribute('href');
        if (!rawHref || rawHref.charAt(0) === '#') {
            return;
        }
        event.preventDefault();
        // Desliga a expansão: o menu recolhe pela transição existente.
        sidebar.classList.add('is-collapsing');
        // Navega ao fim da transição de largura (--transition-base = 150ms).
        window.setTimeout(function () {
            window.location.href = link.href;
        }, 180);
    });
})();

(function () {
    var sidebar = document.getElementById('appSidebar');
    var toggle = document.getElementById('sidebarToggle');
    if (!sidebar || !toggle) {
        return;
    }

    var backdrop = null;

    function openSidebar() {
        sidebar.classList.add('is-open');
        toggle.setAttribute('aria-expanded', 'true');
        backdrop = document.createElement('div');
        backdrop.className = 'app-sidebar-backdrop';
        backdrop.addEventListener('click', closeSidebar);
        document.body.appendChild(backdrop);
    }

    function closeSidebar() {
        sidebar.classList.remove('is-open');
        toggle.setAttribute('aria-expanded', 'false');
        if (backdrop) {
            backdrop.remove();
            backdrop = null;
        }
    }

    toggle.addEventListener('click', function () {
        if (sidebar.classList.contains('is-open')) {
            closeSidebar();
        } else {
            openSidebar();
        }
    });

    var closeBtn = document.getElementById('sidebarClose');
    if (closeBtn) {
        closeBtn.addEventListener('click', closeSidebar);
    }

    // Navegar fecha a sidebar (mobile).
    sidebar.addEventListener('click', function (event) {
        if (event.target.closest('a')) {
            closeSidebar();
        }
    });

    // Esc fecha a sidebar aberta.
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && sidebar.classList.contains('is-open')) {
            closeSidebar();
        }
    });
})();

// ============================================================
// Tooltips do Bootstrap — ativação global. O componente é opt-in
// (não inicializa sozinho), então varremos os elementos marcados
// uma vez por página. Usado pelo ícone de ajuda dos rótulos de
// formulário (partial _FieldHelp).
// ============================================================
(function () {
    if (!window.bootstrap) {
        return;
    }

    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (element) {
        window.bootstrap.Tooltip.getOrCreateInstance(element);
    });
})();
