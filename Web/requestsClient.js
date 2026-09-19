/*
 * Media Requests for Jellyfin: client script.
 * Served by the plugin at /MediaRequests/Client.js and added to the web UI's index.html.
 * Adds a "Requests" button to every page and opens a panel with Find, Requests and (admins) Settings.
 */
(function () {
    'use strict';

    if (window.__mediaRequestsClient) { return; }
    window.__mediaRequestsClient = true;

    var FALLBACK_ERROR = 'Something went wrong. Try again.';
    var HIDDEN_ROUTES = /^#!?\/?(video|login|selectserver|wizard|forgotpassword)/i;
    var STATUS_ORDER = ['Pending', 'Approved', 'Available', 'Declined'];

    var ICON = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zm-2 10h-4v4h-2v-4H7v-2h4V7h2v4h4v2z"/></svg>';

    var CSS = [
        '.mrx-fab{position:fixed;right:1.25em;bottom:1.5em;z-index:1000;display:inline-flex;align-items:center;gap:.5em;padding:.7em 1.15em;border:0;border-radius:99px;background:#0a7aa1;color:#fff;font:inherit;font-size:.95em;font-weight:600;cursor:pointer;box-shadow:0 4px 14px rgba(0,0,0,.45)}',
        '.mrx-fab:hover{background:#0c8bb7}',
        '.mrx-fab svg{width:1.3em;height:1.3em;fill:currentColor}',
        '.mrx-fab[hidden],.mrx-overlay[hidden],.mrx [hidden]{display:none!important}',
        '.mrx-overlay{position:fixed;top:0;right:0;bottom:0;left:0;z-index:100000;background:rgba(0,0,0,.72);display:flex;justify-content:center}',
        '.mrx{--mrx-accent:#00a4dc;--mrx-fill:#0a7aa1;--mrx-line:rgba(255,255,255,.2);--mrx-soft:rgba(255,255,255,.08);box-sizing:border-box;width:min(1100px,100%);height:100%;overflow-y:auto;background:#141414;color:#eee;padding:1.2em 1.6em 3em;font-family:inherit;font-size:1rem;line-height:1.4}',
        '.mrx *{box-sizing:border-box}',
        '.mrx-head{display:flex;align-items:flex-start;justify-content:space-between;gap:1em}',
        '.mrx-title{margin:0 0 .2em;font-size:1.8em;font-weight:600}',
        '.mrx-sub{margin:0 0 1.3em;opacity:.72;max-width:38em}',
        '.mrx-close{flex:none;background:none;border:1px solid var(--mrx-line);color:inherit;border-radius:6px;width:2.4em;height:2.4em;font-size:1.1em;cursor:pointer}',
        '.mrx-tabs{display:flex;gap:1.8em;border-bottom:1px solid var(--mrx-line);margin-bottom:1.4em}',
        '.mrx-tab{background:none;border:0;border-bottom:3px solid transparent;margin-bottom:-1px;color:inherit;font:inherit;font-size:1.05em;padding:.6em 0;cursor:pointer;opacity:.7;display:inline-flex;align-items:center;gap:.5em}',
        '.mrx-tab.is-active{opacity:1;border-bottom-color:var(--mrx-accent)}',
        '.mrx-badge{background:var(--mrx-fill);color:#fff;border-radius:99px;font-size:.78em;min-width:1.6em;padding:.1em .5em;text-align:center}',
        '.mrx button:focus-visible,.mrx a:focus-visible,.mrx input:focus-visible,.mrx-fab:focus-visible{outline:2px solid var(--mrx-accent);outline-offset:2px}',
        '.mrx-input{width:100%;background:var(--mrx-soft);color:inherit;border:1px solid var(--mrx-line);border-radius:6px;padding:.75em 1em;font:inherit}',
        '.mrx-searchrow{display:flex;flex-direction:column;gap:.9em;margin-bottom:1.4em}',
        '.mrx-chips{display:flex;flex-wrap:wrap;gap:.5em;margin-bottom:1.2em}',
        '.mrx-searchrow .mrx-chips{margin-bottom:0}',
        '.mrx-chip{background:transparent;color:inherit;font:inherit;font-size:.92em;cursor:pointer;border:1px solid var(--mrx-line);border-radius:99px;padding:.35em .95em}',
        '.mrx-chip.is-active{background:var(--mrx-fill);border-color:var(--mrx-fill);color:#fff}',
        '.mrx-grid{display:grid;gap:1em;grid-template-columns:repeat(auto-fill,minmax(min(100%,340px),1fr))}',
        '.mrx-card{display:flex;gap:1em;padding:.9em;border:1px solid var(--mrx-line);border-radius:8px}',
        '.mrx-poster{flex:0 0 92px;aspect-ratio:2/3;border-radius:4px;overflow:hidden;background:var(--mrx-soft)}',
        '.mrx-poster img{width:100%;height:100%;object-fit:cover;display:block}',
        '.mrx-noposter{width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:2em;opacity:.4}',
        '.mrx-info{flex:1;min-width:0;display:flex;flex-direction:column}',
        '.mrx-name{margin:0;font-size:1.05em;font-weight:600;line-height:1.3}',
        '.mrx-meta{display:flex;gap:.6em;align-items:center;margin-top:.25em;font-size:.88em;opacity:.75}',
        '.mrx-type{border:1px solid var(--mrx-line);border-radius:4px;padding:0 .4em}',
        '.mrx-overview{margin:.5em 0 .8em;font-size:.9em;opacity:.75;line-height:1.4;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden}',
        '.mrx-by{margin:.4em 0 0;font-size:.85em;opacity:.7}',
        '.mrx-actions{margin-top:auto;padding-top:.6em;display:flex;flex-wrap:wrap;gap:.5em;align-items:center}',
        '.mrx-btn{display:inline-block;padding:.45em .95em;border-radius:5px;border:1px solid var(--mrx-line);background:transparent;color:inherit;font:inherit;font-size:.92em;cursor:pointer;text-decoration:none}',
        '.mrx-btn[disabled]{opacity:.55;cursor:default}',
        '.mrx-btn-primary{background:var(--mrx-fill);border-color:var(--mrx-fill);color:#fff}',
        '.mrx-btn-danger{color:#e2716d;border-color:rgba(226,113,109,.55)}',
        '.mrx-status{display:inline-flex;align-items:center;padding:.2em .7em;border-radius:99px;font-size:.85em;border:1px solid currentColor}',
        '.mrx-status-Pending{color:#e0a838}.mrx-status-Approved{color:#4fb0e0}.mrx-status-Available{color:#55b676}.mrx-status-Declined{color:#e2716d}',
        '.mrx-decline{display:flex;gap:.5em;flex-wrap:wrap;width:100%}',
        '.mrx-decline .mrx-input{flex:1 1 12em;padding:.45em .7em;font-size:.92em}',
        '.mrx-empty{opacity:.7;padding:1.5em 0;max-width:34em}',
        '.mrx-error{color:#e2716d;padding:1.5em 0;max-width:34em}',
        '.mrx-banner{display:flex;flex-wrap:wrap;gap:.8em;align-items:center;border:1px solid var(--mrx-line);border-left:4px solid #e0a838;border-radius:6px;padding:.8em 1em;margin-bottom:1.2em}',
        '.mrx-form{max-width:34em;display:flex;flex-direction:column;gap:1.2em}',
        '.mrx-field label{display:block;font-weight:600;margin-bottom:.35em}',
        '.mrx-hint{margin:.35em 0 0;font-size:.88em;opacity:.7}',
        '.mrx-check{display:flex;gap:.6em;align-items:center}',
        '.mrx-msg{margin:0;min-height:1.4em}',
        '.mrx-msg.is-ok{color:#55b676}.mrx-msg.is-err{color:#e2716d}',
        '.mrx-toast{position:fixed;left:50%;bottom:2em;transform:translateX(-50%);z-index:100001;background:#262626;color:#fff;padding:.75em 1.2em;border-radius:6px;max-width:90vw;box-shadow:0 4px 16px rgba(0,0,0,.5);opacity:0;pointer-events:none;transition:opacity .2s}',
        '.mrx-toast.is-visible{opacity:1}.mrx-toast.is-error{background:#a83a36}',
        '@media (prefers-reduced-motion:reduce){.mrx-toast{transition:none}}'
    ].join('\n');

    var TEMPLATE =
        '<div class="mrx" role="dialog" aria-modal="true" aria-labelledby="mrxTitle" tabindex="-1">' +
        '<div class="mrx-head"><div><h1 class="mrx-title" id="mrxTitle">Requests</h1>' +
        '<p class="mrx-sub">Can\'t find something in the library? Search for it and request it. An admin will review your request.</p></div>' +
        '<button type="button" class="mrx-close" data-action="close" aria-label="Close">&#10005;</button></div>' +
        '<div class="mrx-tabs" role="tablist">' +
        '<button type="button" class="mrx-tab is-active" role="tab" aria-selected="true" data-view="find">Find</button>' +
        '<button type="button" class="mrx-tab" role="tab" aria-selected="false" data-view="requests"><span id="mrxRequestsLabel">My requests</span><span id="mrxBadge" class="mrx-badge" hidden></span></button>' +
        '<button type="button" class="mrx-tab" role="tab" aria-selected="false" data-view="settings" id="mrxSettingsTab" hidden>Settings</button>' +
        '</div>' +
        '<section id="mrxFind">' +
        '<div id="mrxBanner"></div>' +
        '<div class="mrx-searchrow">' +
        '<input id="mrxQuery" class="mrx-input" type="search" autocomplete="off" placeholder="Search movies and shows" aria-label="Search movies and shows">' +
        '<div class="mrx-chips" id="mrxTypeChips">' +
        '<button type="button" class="mrx-chip is-active" data-type="all">All</button>' +
        '<button type="button" class="mrx-chip" data-type="movie">Movies</button>' +
        '<button type="button" class="mrx-chip" data-type="tv">Shows</button></div></div>' +
        '<div id="mrxFindBody" aria-live="polite"></div></section>' +
        '<section id="mrxRequests" hidden><div class="mrx-chips" id="mrxStatusChips"></div><div id="mrxRequestsBody"></div></section>' +
        '<section id="mrxSettings" hidden>' +
        '<form class="mrx-form" id="mrxSettingsForm">' +
        '<div class="mrx-field"><label for="mrxKey">TMDB API key or read access token</label>' +
        '<input id="mrxKey" class="mrx-input" type="password" autocomplete="off" spellcheck="false">' +
        '<p class="mrx-hint" id="mrxKeyHint"></p></div>' +
        '<div class="mrx-field"><label for="mrxLang">Search language</label>' +
        '<input id="mrxLang" class="mrx-input" type="text" placeholder="en-US">' +
        '<p class="mrx-hint">For example en-US, de-DE, or ja-JP.</p></div>' +
        '<div class="mrx-field"><label for="mrxMax">Pending requests per user</label>' +
        '<input id="mrxMax" class="mrx-input" type="number" min="0" step="1">' +
        '<p class="mrx-hint">How many unreviewed requests one person can have at once. Use 0 for no limit.</p></div>' +
        '<label class="mrx-check"><input id="mrxAdult" type="checkbox"><span>Include adult titles in search results</span></label>' +
        '<div><button type="submit" class="mrx-btn mrx-btn-primary">Save and test</button></div>' +
        '<p class="mrx-msg" id="mrxSettingsMsg" role="status"></p>' +
        '</form></section>' +
        '<div class="mrx-toast" id="mrxToast" role="status" aria-live="polite"></div>' +
        '</div>';

    var state = {
        open: false, view: 'find', type: 'all', query: '',
        find: { status: 'idle', message: '', items: [] },
        requests: null, isAdmin: false, needsSetup: false, filter: null, declining: null, loadError: '',
        settings: null
    };
    var fab, overlay, panel, lastFocus, searchSeq = 0, debounceTimer = null, toastTimer = null;

    function $(sel) { return overlay.querySelector(sel); }

    function esc(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function isSignedIn() {
        try { return !!(window.ApiClient && ApiClient.getCurrentUserId && ApiClient.getCurrentUserId()); }
        catch (e) { return false; }
    }

    function api(method, path, body) {
        var options = { type: method, url: ApiClient.getUrl('MediaRequests/' + path), dataType: 'json' };
        if (body !== undefined) {
            options.data = JSON.stringify(body);
            options.contentType = 'application/json';
        }
        return ApiClient.ajax(options);
    }

    function errorText(err) {
        if (err && typeof err.json === 'function') {
            return err.json().then(
                function (j) { return (j && (j.message || j.detail || j.title)) || FALLBACK_ERROR; },
                function () { return FALLBACK_ERROR; }
            );
        }
        return Promise.resolve(FALLBACK_ERROR);
    }

    function toast(message, isError) {
        var el = $('#mrxToast');
        el.textContent = message;
        el.className = 'mrx-toast is-visible' + (isError ? ' is-error' : '');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { el.className = 'mrx-toast'; }, 4000);
    }

    function typeLabel(type) { return type === 'tv' ? 'Show' : 'Movie'; }

    function statusChip(status, text) {
        return '<span class="mrx-status mrx-status-' + esc(status) + '">' + esc(text || status) + '</span>';
    }

    function card(m) {
        var initial = esc((m.title || '?').charAt(0));
        var poster = m.posterUrl
            ? '<img src="' + esc(m.posterUrl) + '" alt="" loading="lazy" referrerpolicy="no-referrer" data-initial="' + initial + '">'
            : '<div class="mrx-noposter" aria-hidden="true">' + initial + '</div>';
        return '<article class="mrx-card" data-card="' + esc(m.id) + '">' +
            '<div class="mrx-poster">' + poster + '</div>' +
            '<div class="mrx-info">' +
            '<h3 class="mrx-name">' + esc(m.title) + '</h3>' +
            '<div class="mrx-meta"><span class="mrx-type">' + typeLabel(m.mediaType) + '</span>' +
            (m.year ? '<span>' + esc(m.year) + '</span>' : '') + '</div>' +
            (m.overview ? '<p class="mrx-overview">' + esc(m.overview) + '</p>' : '') +
            (m.extra || '') +
            '<div class="mrx-actions">' + m.actions + '</div>' +
            '</div></article>';
    }

    // ---------- Overlay lifecycle ----------

    function syncFab() {
        if (!fab) { return; }
        fab.hidden = state.open || !isSignedIn() || HIDDEN_ROUTES.test(location.hash || '') || !!document.fullscreenElement;
    }

    function openOverlay() {
        if (!isSignedIn()) { return; }
        lastFocus = document.activeElement;
        state.open = true;
        overlay.hidden = false;
        document.documentElement.style.overflow = 'hidden';
        renderTabs();
        renderFind();
        loadRequests();
        syncFab();
        setTimeout(function () { var q = $('#mrxQuery'); if (q && state.view === 'find') { q.focus(); } else { panel.focus(); } }, 50);
    }

    function closeOverlay() {
        if (!state.open) { return; }
        state.open = false;
        overlay.hidden = true;
        document.documentElement.style.overflow = '';
        state.declining = null;
        syncFab();
        if (lastFocus && lastFocus.focus) { try { lastFocus.focus(); } catch (e) { /* ignore */ } }
    }

    function showView(view) {
        state.view = view;
        renderTabs();
        if (view === 'requests') { renderRequests(); loadRequests(); }
        if (view === 'settings') { loadSettings(); }
        if (view === 'find') { renderFind(); }
    }

    // ---------- Find ----------

    function findActions(r) {
        if (r.inLibrary) {
            return '<a class="mrx-btn mrx-btn-primary" data-action="open-item" href="#/details?id=' + esc(r.libraryItemId) + '">Open in library</a>';
        }
        if (r.requestStatus) {
            return statusChip(r.requestStatus, r.requestedByMe ? 'Requested by you' : 'Already requested');
        }
        return '<button type="button" class="mrx-btn mrx-btn-primary" data-action="request" data-tmdb="' +
            esc(r.tmdbId) + '" data-type="' + esc(r.mediaType) + '">Request</button>';
    }

    function renderBanner() {
        var el = $('#mrxBanner');
        if (state.isAdmin && state.needsSetup) {
            el.innerHTML = '<div class="mrx-banner"><span>Search needs a TMDB API key before anyone can use it.</span>' +
                '<button type="button" class="mrx-btn mrx-btn-primary" data-action="open-settings">Add your key</button></div>';
        } else {
            el.innerHTML = '';
        }
    }

    function renderFind() {
        renderBanner();
        var body = $('#mrxFindBody');
        var f = state.find;
        if (f.status === 'idle') {
            body.innerHTML = '<p class="mrx-empty">Type a title to search movies and shows.</p>';
        } else if (f.status === 'loading') {
            body.innerHTML = '<p class="mrx-empty">Searching…</p>';
        } else if (f.status === 'error') {
            body.innerHTML = '<p class="mrx-error">' + esc(f.message) + '</p>' +
                (state.isAdmin && f.needsSetup ? '<button type="button" class="mrx-btn mrx-btn-primary" data-action="open-settings">Open settings</button>' : '');
        } else if (!f.items.length) {
            body.innerHTML = '<p class="mrx-empty">No matches for "' + esc(state.query.trim()) + '". Check the spelling or try another title.</p>';
        } else {
            body.innerHTML = '<div class="mrx-grid">' + f.items.map(function (r) {
                return card({
                    id: r.mediaType + '-' + r.tmdbId, title: r.title, year: r.year, mediaType: r.mediaType,
                    overview: r.overview, posterUrl: r.posterUrl, actions: findActions(r)
                });
            }).join('') + '</div>';
        }
    }

    function runSearch() {
        var q = state.query.trim();
        if (q.length < 2) {
            searchSeq++;
            state.find = { status: 'idle', message: '', items: [] };
            renderFind();
            return;
        }
        var seq = ++searchSeq;
        state.find = { status: 'loading', message: '', items: [] };
        renderFind();
        api('GET', 'Search?query=' + encodeURIComponent(q) + '&mediaType=' + state.type).then(function (items) {
            if (seq !== searchSeq) { return; }
            state.find = { status: 'done', message: '', items: items || [] };
            renderFind();
        }).catch(function (err) {
            errorText(err).then(function (message) {
                if (seq !== searchSeq) { return; }
                state.find = { status: 'error', message: message, items: [], needsSetup: !!(err && err.status === 503) };
                renderFind();
            });
        });
    }

    function requestTitle(button) {
        var tmdbId = parseInt(button.getAttribute('data-tmdb'), 10);
        var mediaType = button.getAttribute('data-type');
        button.disabled = true;
        button.textContent = 'Requesting…';

        api('POST', 'Requests', { tmdbId: tmdbId, mediaType: mediaType }).then(function (created) {
            state.find.items.forEach(function (r) {
                if (r.tmdbId === tmdbId && r.mediaType === mediaType) {
                    r.requestStatus = 'Pending';
                    r.requestedByMe = true;
                }
            });
            if (state.requests) { state.requests.unshift(created); }
            renderFind();
            renderTabs();
            toast('Requested "' + created.title + '". An admin will review it.');
        }).catch(function (err) {
            errorText(err).then(function (message) {
                toast(message, true);
                if (err && err.status === 409) { runSearch(); return; }
                button.disabled = false;
                button.textContent = 'Request';
            });
        });
    }

    // ---------- Requests ----------

    function requestActions(r) {
        var html = statusChip(r.status);
        var id = esc(r.id);
        if (r.status === 'Available' && r.libraryItemId) {
            html += '<a class="mrx-btn mrx-btn-primary" data-action="open-item" href="#/details?id=' + esc(r.libraryItemId) + '">Open in library</a>';
        }
        if (state.isAdmin) {
            if (state.declining === r.id) {
                return '<div class="mrx-decline">' +
                    '<input class="mrx-input" type="text" maxlength="300" data-role="reason" placeholder="Reason (optional)" aria-label="Reason for declining">' +
                    '<button type="button" class="mrx-btn mrx-btn-danger" data-action="decline-confirm" data-id="' + id + '">Decline</button>' +
                    '<button type="button" class="mrx-btn" data-action="decline-cancel">Cancel</button></div>';
            }
            if (r.status === 'Pending') {
                html += '<button type="button" class="mrx-btn mrx-btn-primary" data-action="status" data-to="Approved" data-id="' + id + '">Approve</button>';
                html += '<button type="button" class="mrx-btn" data-action="decline" data-id="' + id + '">Decline</button>';
            } else if (r.status === 'Approved') {
                html += '<button type="button" class="mrx-btn" data-action="status" data-to="Available" data-id="' + id + '">Mark available</button>';
                html += '<button type="button" class="mrx-btn" data-action="decline" data-id="' + id + '">Decline</button>';
            } else if (r.status === 'Declined') {
                html += '<button type="button" class="mrx-btn" data-action="status" data-to="Pending" data-id="' + id + '">Reopen</button>';
            }
            html += '<button type="button" class="mrx-btn mrx-btn-danger" data-action="delete" data-id="' + id + '">Delete</button>';
        } else if (r.status === 'Pending') {
            html += '<button type="button" class="mrx-btn" data-action="delete" data-id="' + id + '">Cancel request</button>';
        }
        return html;
    }

    function requestExtra(r) {
        var when = new Date(r.createdUtc).toLocaleDateString();
        var html = '<p class="mrx-by">' + (state.isAdmin ? 'Requested by ' + esc(r.userName) + ' on ' : 'Requested on ') + esc(when) + '</p>';
        if (r.adminNote) {
            html += '<p class="mrx-by">' + (r.status === 'Declined' ? 'Reason: ' : 'Note: ') + esc(r.adminNote) + '</p>';
        }
        return html;
    }

    function counts() {
        var c = { all: 0, Pending: 0, Approved: 0, Available: 0, Declined: 0 };
        (state.requests || []).forEach(function (r) { c.all++; c[r.status]++; });
        return c;
    }

    function renderTabs() {
        Array.prototype.forEach.call(overlay.querySelectorAll('.mrx-tab'), function (tab) {
            var active = tab.getAttribute('data-view') === state.view;
            tab.classList.toggle('is-active', active);
            tab.setAttribute('aria-selected', active ? 'true' : 'false');
        });
        $('#mrxFind').hidden = state.view !== 'find';
        $('#mrxRequests').hidden = state.view !== 'requests';
        $('#mrxSettings').hidden = state.view !== 'settings';
        $('#mrxSettingsTab').hidden = !state.isAdmin;
        $('#mrxRequestsLabel').textContent = state.isAdmin ? 'Requests' : 'My requests';

        var pending = counts().Pending;
        var badge = $('#mrxBadge');
        badge.hidden = !(state.isAdmin && pending > 0);
        badge.textContent = pending;
    }

    function renderRequests() {
        var chips = $('#mrxStatusChips');
        var body = $('#mrxRequestsBody');

        if (state.requests === null) {
            chips.innerHTML = '';
            body.innerHTML = state.loadError
                ? '<p class="mrx-error">' + esc(state.loadError) + '</p>'
                : '<p class="mrx-empty">Loading…</p>';
            return;
        }

        var c = counts();
        var filters = [['all', 'All']].concat(STATUS_ORDER.map(function (s) { return [s, s]; }));
        chips.innerHTML = filters.map(function (f) {
            return '<button type="button" class="mrx-chip' + (state.filter === f[0] ? ' is-active' : '') +
                '" data-status="' + f[0] + '">' + f[1] + ' (' + c[f[0]] + ')</button>';
        }).join('');

        var list = state.requests.filter(function (r) { return state.filter === 'all' || r.status === state.filter; });
        if (!list.length) {
            var text;
            if (!state.requests.length) {
                text = state.isAdmin
                    ? 'No one has requested anything yet.'
                    : 'You haven\'t requested anything yet. Use Find to search for a movie or show.';
            } else {
                text = 'Nothing here. Pick another status above.';
            }
            body.innerHTML = '<p class="mrx-empty">' + text + '</p>';
            return;
        }

        body.innerHTML = '<div class="mrx-grid">' + list.map(function (r) {
            return card({
                id: r.id, title: r.title, year: r.year, mediaType: r.mediaType, overview: r.overview,
                posterUrl: r.posterUrl, extra: requestExtra(r), actions: requestActions(r)
            });
        }).join('') + '</div>';
    }

    function loadRequests() {
        return api('GET', 'Requests').then(function (res) {
            state.isAdmin = !!res.isAdmin;
            state.needsSetup = !!res.needsSetup;
            state.requests = res.requests || [];
            state.loadError = '';
            if (state.filter === null) { state.filter = state.isAdmin ? 'Pending' : 'all'; }
            renderTabs();
            renderBanner();
            if (state.view === 'requests') { renderRequests(); }
        }).catch(function (err) {
            return errorText(err).then(function (message) {
                state.loadError = message;
                if (state.view === 'requests') { renderRequests(); }
            });
        });
    }

    function replaceRequest(updated) {
        state.requests = state.requests.map(function (r) { return r.id === updated.id ? updated : r; });
    }

    function setStatus(id, status, adminNote) {
        api('PUT', 'Requests/' + id + '/Status', { status: status, adminNote: adminNote || null }).then(function (updated) {
            replaceRequest(updated);
            state.declining = null;
            renderTabs();
            renderRequests();
            toast(status === 'Pending' ? 'Request reopened.' : 'Marked as ' + status.toLowerCase() + '.');
        }).catch(function (err) {
            errorText(err).then(function (message) { toast(message, true); });
        });
    }

    function deleteRequest(id) {
        if (!window.confirm(state.isAdmin ? 'Delete this request?' : 'Cancel this request?')) { return; }
        api('DELETE', 'Requests/' + id).then(function () {
            state.requests = state.requests.filter(function (x) { return x.id !== id; });
            renderTabs();
            renderRequests();
            toast(state.isAdmin ? 'Request deleted.' : 'Request cancelled.');
            if (state.find.status === 'done') { runSearch(); }
        }).catch(function (err) {
            errorText(err).then(function (message) { toast(message, true); });
        });
    }

    // ---------- Settings (admins) ----------

    function setMsg(text, kind) {
        var el = $('#mrxSettingsMsg');
        el.textContent = text || '';
        el.className = 'mrx-msg' + (kind === 'ok' ? ' is-ok' : kind === 'err' ? ' is-err' : '');
    }

    function renderSettings() {
        var s = state.settings;
        if (!s) { return; }
        $('#mrxLang').value = s.language || 'en-US';
        $('#mrxMax').value = s.maxPendingRequestsPerUser;
        $('#mrxAdult').checked = !!s.includeAdultResults;
        $('#mrxKeyHint').textContent = s.hasApiKey
            ? 'A key is saved. Leave this blank to keep it, or enter a new one to replace it.'
            : 'Get a free key at themoviedb.org under Settings, then API. The API key and the API read access token both work. The key stays on your server.';
    }

    function loadSettings() {
        setMsg('');
        return api('GET', 'Settings').then(function (s) {
            state.settings = s;
            renderSettings();
        }).catch(function (err) {
            errorText(err).then(function (m) { setMsg(m, 'err'); });
        });
    }

    function saveSettings(e) {
        e.preventDefault();
        var max = parseInt($('#mrxMax').value, 10);
        var body = {
            tmdbApiKey: $('#mrxKey').value.trim() || null,
            tmdbLanguage: $('#mrxLang').value.trim() || 'en-US',
            maxPendingRequestsPerUser: isNaN(max) || max < 0 ? 0 : max,
            includeAdultResults: $('#mrxAdult').checked
        };
        setMsg('Saving…');
        api('PUT', 'Settings', body).then(function (s) {
            state.settings = s;
            state.needsSetup = !s.hasApiKey;
            $('#mrxKey').value = '';
            renderSettings();
            renderBanner();
            if (!s.hasApiKey) { setMsg('Saved. Add a TMDB key to turn on search.'); return; }
            setMsg('Saved. Checking the TMDB connection…');
            return api('POST', 'Settings/Test').then(function () {
                setMsg('Saved. The TMDB connection works.', 'ok');
                if (state.find.status === 'error') { state.find = { status: 'idle', message: '', items: [] }; renderFind(); }
            }).catch(function (err) {
                return errorText(err).then(function (m) { setMsg('Saved, but the TMDB check failed: ' + m, 'err'); });
            });
        }).catch(function (err) {
            errorText(err).then(function (m) { setMsg(m, 'err'); });
        });
    }

    // ---------- Events ----------

    function onClick(e) {
        if (e.target === overlay) { closeOverlay(); return; }

        var tab = e.target.closest('.mrx-tab');
        if (tab) { showView(tab.getAttribute('data-view')); return; }

        var typeChip = e.target.closest('#mrxTypeChips .mrx-chip');
        if (typeChip) {
            state.type = typeChip.getAttribute('data-type');
            Array.prototype.forEach.call(overlay.querySelectorAll('#mrxTypeChips .mrx-chip'), function (c) {
                c.classList.toggle('is-active', c === typeChip);
            });
            runSearch();
            return;
        }

        var statusEl = e.target.closest('#mrxStatusChips .mrx-chip');
        if (statusEl) {
            state.filter = statusEl.getAttribute('data-status');
            state.declining = null;
            renderRequests();
            return;
        }

        var btn = e.target.closest('[data-action]');
        if (!btn) { return; }
        var action = btn.getAttribute('data-action');
        var id = btn.getAttribute('data-id');

        if (action === 'close') { closeOverlay(); }
        else if (action === 'open-item') { closeOverlay(); }
        else if (action === 'open-settings') { showView('settings'); }
        else if (action === 'request') { requestTitle(btn); }
        else if (action === 'status') { setStatus(id, btn.getAttribute('data-to')); }
        else if (action === 'decline') { state.declining = id; renderRequests(); }
        else if (action === 'decline-cancel') { state.declining = null; renderRequests(); }
        else if (action === 'decline-confirm') {
            var reason = btn.closest('.mrx-card').querySelector('[data-role="reason"]').value.trim();
            setStatus(id, 'Declined', reason);
        }
        else if (action === 'delete') { deleteRequest(id); }
    }

    function start() {
        var style = document.createElement('style');
        style.textContent = CSS;
        document.head.appendChild(style);

        fab = document.createElement('button');
        fab.type = 'button';
        fab.className = 'mrx-fab';
        fab.hidden = true;
        fab.setAttribute('aria-label', 'Request a movie or show');
        fab.innerHTML = ICON + '<span>Requests</span>';
        fab.addEventListener('click', openOverlay);
        document.body.appendChild(fab);

        overlay = document.createElement('div');
        overlay.className = 'mrx-overlay';
        overlay.hidden = true;
        overlay.innerHTML = TEMPLATE;
        document.body.appendChild(overlay);
        panel = overlay.querySelector('.mrx');

        overlay.addEventListener('click', onClick);
        overlay.addEventListener('error', function (e) {
            var t = e.target;
            if (t && t.tagName === 'IMG') {
                var ph = document.createElement('div');
                ph.className = 'mrx-noposter';
                ph.setAttribute('aria-hidden', 'true');
                ph.textContent = t.getAttribute('data-initial') || '?';
                t.parentNode.replaceChild(ph, t);
            }
        }, true);

        $('#mrxQuery').addEventListener('input', function (e) {
            state.query = e.target.value;
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(runSearch, 350);
        });
        $('#mrxQuery').addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { clearTimeout(debounceTimer); runSearch(); }
        });
        $('#mrxSettingsForm').addEventListener('submit', saveSettings);

        document.addEventListener('keydown', function (e) {
            if (state.open && e.key === 'Escape') { closeOverlay(); }
        });
        window.addEventListener('hashchange', function () {
            if (state.open) { closeOverlay(); }
            syncFab();
        });
        document.addEventListener('fullscreenchange', syncFab);
        setInterval(syncFab, 1500);
        syncFab();
    }

    if (document.body) { start(); }
    else { document.addEventListener('DOMContentLoaded', start); }
})();
