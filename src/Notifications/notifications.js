// Notifications: runs in the game's root web page, above the pages the game draws in iframes. It adds a Notifications
// app to the phone (the PhoneSMS page): an icon on the phone's dock with an unread dot, and a list of every notice of
// this save, newest first. The plugin hands it the list with set(); the player's clicks go back to the plugin as
// vuplex messages. All text goes on the page with textContent, so no notice can run as script or markup.
(function () {
    if (window.__slnotify) return;

    var PREFIX = 'slmod|notifications|';
    var SVG = 'http://www.w3.org/2000/svg';
    var BELL = 'M6 9a6 6 0 0 1 12 0c0 5 2 7 2 7H4s2-2 2-7z M10 19.5a2 2 0 0 0 4 0';
    var BACK = 'M15 6l-6 6 6 6';
    var STYLE_ID = 'slnotify-style';
    var CHECK_MS = 500;
    var CSS =
        '.app-dock{flex-wrap:wrap;row-gap:12px}' +
        '.app.is-slnotify .app-icon{background:linear-gradient(180deg,#9cc3a4 0%,#4f7a5a 100%);border:1px solid rgba(0,0,0,0.25);' +
        'box-shadow:inset 0 1px 0 rgba(230,255,235,0.5),inset 0 -2px 4px rgba(0,40,10,0.3),0 4px 10px rgba(0,0,0,0.4)}' +
        '.app.is-slnotify .unread-dot.slnotify-hidden{display:none}' +
        '.view.slnotify-view{z-index:2;background:radial-gradient(circle at center bottom,rgba(251,176,52,0.20) 0%,transparent 62%),' +
        'linear-gradient(135deg,rgba(65,52,45,0.96) 0%,rgba(35,28,24,0.98) 100%)}' +
        '.slnotify-view .list-row{cursor:default;align-items:flex-start}' +
        '.slnotify-view .list-row.is-clickable{cursor:pointer}' +
        '.slnotify-view .list-row.is-unread{box-shadow:inset 3px 0 0 var(--accent,#fbb034)}' +
        '.slnotify-view .row-preview{white-space:normal;max-width:none;line-height:1.5}' +
        '.slnotify-view .slnotify-count{font-size:11px;font-weight:700;color:var(--accent,#fbb034)}' +
        '.slnotify-view .slnotify-source{font-size:10px;color:var(--text-faint,#8a7f72);white-space:nowrap}' +
        '.slnotify-view .list-empty.slnotify-hidden,.slnotify-view .list-rows.slnotify-hidden{display:none}';

    var view = { label: 'Notifications', empty: '', unread: 0, mark: 0, rows: [] };
    var revision = 0;
    var reported = {};

    function post(message) {
        try {
            if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
        } catch (e) {
            // Nothing to tell anyone with; the next check tries again.
        }
    }

    // Each distinct problem is reported once, so a broken frame cannot flood the log.
    function report(text) {
        if (reported[text]) return;

        reported[text] = true;
        post('diag|' + text);
    }

    // The PhoneSMS frames this page can reach. A frame from another origin throws on reading its location; it is not
    // the phone, so it is passed over.
    function phoneFrames() {
        var found = [];
        var frames = document.querySelectorAll('iframe');

        for (var i = 0; i < frames.length; i++) {
            var win = frames[i].contentWindow;

            if (!win) continue;

            try {
                if (/PhoneSMS\.html/i.test(String(win.location)) && win.document) found.push({ frame: frames[i], win: win, doc: win.document });
            } catch (e) {
                // Not ours to read.
            }
        }

        return found;
    }

    function element(doc, tag, className, text) {
        var made = doc.createElement(tag);

        if (className) made.className = className;
        if (text !== undefined) made.textContent = text;

        return made;
    }

    function icon(doc, path) {
        var svg = doc.createElementNS ? doc.createElementNS(SVG, 'svg') : doc.createElement('svg');
        var line = doc.createElementNS ? doc.createElementNS(SVG, 'path') : doc.createElement('path');

        svg.setAttribute('viewBox', '0 0 24 24');
        line.setAttribute('d', path);
        line.setAttribute('stroke-linecap', 'round');
        line.setAttribute('stroke-linejoin', 'round');
        svg.appendChild(line);

        return svg;
    }

    function listen(target, type, handler) {
        target.addEventListener(type, function (event) {
            try {
                handler(event);
            } catch (e) {
                report(type + ' handler failed: ' + e);
            }
        });
    }

    // Puts back whatever of the app the phone's own redraws took away. Returns true when something was added.
    function ensure(phone) {
        var doc = phone.doc;
        var isChanged = false;

        if (!doc.getElementById(STYLE_ID) && (doc.head || doc.body)) {
            var style = element(doc, 'style');

            style.id = STYLE_ID;
            style.textContent = CSS;
            (doc.head || doc.body).appendChild(style);
            isChanged = true;
        }

        var dock = doc.querySelector('.app-dock');

        if (dock && !dock.querySelector('.is-slnotify')) {
            dock.appendChild(buildIcon(phone));
            isChanged = true;
        }

        var panel = doc.querySelector('.sms-panel');

        if (panel && !panel.querySelector('.slnotify-view')) {
            panel.appendChild(buildView(phone));
            isChanged = true;
        }

        var appRoot = doc.querySelector('.app-root');

        if (appRoot && !appRoot.__slnotifyListening) {
            appRoot.__slnotifyListening = true;
            // A click outside the phone's panel closes the phone; the list must not be open when it comes back.
            listen(appRoot, 'click', function () { closeList(doc); });
        }

        return isChanged;
    }

    function buildIcon(phone) {
        var doc = phone.doc;
        var app = element(doc, 'div', 'app is-slnotify');
        var dot = element(doc, 'div', 'unread-dot is-plain slnotify-hidden');
        var tile = element(doc, 'div', 'app-icon');
        var glyph = element(doc, 'div', 'icon-glyph');

        glyph.appendChild(icon(doc, BELL));
        tile.appendChild(glyph);
        app.appendChild(dot);
        app.appendChild(tile);
        app.appendChild(element(doc, 'div', 'app-label', view.label));
        listen(app, 'click', function (event) {
            if (event && event.stopPropagation) event.stopPropagation();

            openList(doc);
        });

        return app;
    }

    function buildView(phone) {
        var doc = phone.doc;
        var panelView = element(doc, 'div', 'view slnotify-view');
        var head = element(doc, 'div', 'panel-head');
        var back = element(doc, 'div', 'back');
        var title = element(doc, 'div', 'title');
        var scroll = element(doc, 'div', 'list-scroll');
        var empty = element(doc, 'div', 'list-empty');

        back.appendChild(icon(doc, BACK));
        listen(back, 'click', function () { closeList(doc); });
        title.appendChild(element(doc, 'span', 't-name', view.label));
        head.appendChild(back);
        head.appendChild(title);
        empty.appendChild(element(doc, 'div', 'empty-text', view.empty));
        scroll.appendChild(element(doc, 'div', 'list-rows'));
        scroll.appendChild(empty);
        panelView.appendChild(head);
        panelView.appendChild(scroll);
        panelView.__slnotifyRevision = -1;

        return panelView;
    }

    function openList(doc) {
        var panelView = doc.querySelector('.slnotify-view');

        if (!panelView) return;

        panelView.classList.add('is-active');
        view.unread = 0;
        drawDot(doc);
        post('seen|' + view.mark);
    }

    function closeList(doc) {
        var panelView = doc.querySelector('.slnotify-view');

        if (panelView) panelView.classList.remove('is-active');
    }

    // The game's own words for "day N" when the phone has them, so the time reads like the phone's clock.
    function dayLabel(doc) {
        try {
            var state = doc.getElementById('app')._vnode.component.setupState.state;
            var label = state && state.localText && state.localText.dayLabel;

            if (label) return String(label);
        } catch (e) {
            // An older or newer phone page; the fallback below reads the same.
        }

        return 'DAY {0}';
    }

    function timeText(doc, row) {
        if (typeof row.day !== 'number' || typeof row.hour !== 'number') return '';

        var hour = row.hour < 10 ? '0' + row.hour : String(row.hour);

        return dayLabel(doc).replace('{0}', String(row.day)) + ' ' + hour + ':00';
    }

    function drawDot(doc) {
        var dot = doc.querySelector('.is-slnotify .unread-dot');

        if (!dot) return;

        if (view.unread > 0) dot.classList.remove('slnotify-hidden');
        else dot.classList.add('slnotify-hidden');
    }

    function buildRow(doc, row) {
        var line = element(doc, 'div', 'list-row slnotify-row');
        var main = element(doc, 'div', 'row-main');
        var name = element(doc, 'div', 'row-name');
        var meta = element(doc, 'div', 'row-meta');

        name.appendChild(element(doc, 'span', 'row-nm', row.title));

        if (row.count > 1) name.appendChild(element(doc, 'span', 'slnotify-count', 'x' + row.count));

        main.appendChild(name);

        if (row.body) main.appendChild(element(doc, 'div', 'row-preview', row.body));

        meta.appendChild(element(doc, 'div', 'row-time', timeText(doc, row)));
        meta.appendChild(element(doc, 'div', 'slnotify-source', row.source));
        line.appendChild(main);
        line.appendChild(meta);
        line.setAttribute('data-seq', String(row.seq));

        if (row.unread) line.classList.add('is-unread');

        if (row.click) {
            line.classList.add('is-clickable');
            listen(line, 'click', function () { openRow(doc, row.seq); });
        }

        return line;
    }

    // A row opens its popup again: the list and the phone close first, so the popup is not drawn under the phone.
    function openRow(doc, seq) {
        closeList(doc);

        var appRoot = doc.querySelector('.app-root');

        if (appRoot && appRoot.click) appRoot.click();

        post('open|' + seq);
    }

    function draw(doc) {
        var panelView = doc.querySelector('.slnotify-view');
        var label = doc.querySelector('.is-slnotify .app-label');

        if (label) label.textContent = view.label;

        drawDot(doc);

        if (!panelView || panelView.__slnotifyRevision === revision) return;

        panelView.__slnotifyRevision = revision;

        var title = panelView.querySelector('.t-name');
        var rows = panelView.querySelector('.list-rows');
        var empty = panelView.querySelector('.list-empty');
        var emptyText = panelView.querySelector('.empty-text');

        if (title) title.textContent = view.label;
        if (emptyText) emptyText.textContent = view.empty;
        if (!rows) return;

        while (rows.firstChild) rows.removeChild(rows.firstChild);

        for (var i = 0; i < view.rows.length; i++) rows.appendChild(buildRow(doc, view.rows[i]));

        if (view.rows.length > 0) {
            rows.classList.remove('slnotify-hidden');
            if (empty) empty.classList.add('slnotify-hidden');
        } else {
            rows.classList.add('slnotify-hidden');
            if (empty) empty.classList.remove('slnotify-hidden');
        }
    }

    // The phone page redraws its dock and panel with Vue; an observer on its body puts the app back the moment a
    // redraw drops it, rather than at the next check.
    function watch(phone) {
        if (phone.doc.__slnotifyObserved) return;

        phone.doc.__slnotifyObserved = true;

        var Observer = phone.win.MutationObserver || window.MutationObserver;

        if (!Observer || !phone.doc.body) return;

        new Observer(function () {
            try {
                if (ensure(phone)) draw(phone.doc);
            } catch (e) {
                report('redraw failed: ' + e);
            }
        }).observe(phone.doc.body, { childList: true, subtree: true });
    }

    function check() {
        var phones = phoneFrames();

        for (var i = 0; i < phones.length; i++) {
            try {
                ensure(phones[i]);
                watch(phones[i]);
                draw(phones[i].doc);
            } catch (e) {
                report('phone update failed: ' + e);
            }
        }
    }

    window.__slnotify = {
        set: function (next) {
            if (!next || !next.rows) return;

            view = next;
            revision++;
            check();
        },
        check: check
    };

    setInterval(check, CHECK_MS);
    check();
})();
