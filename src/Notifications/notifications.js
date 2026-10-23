// Notifications: runs in the game's root web page, above the pages the game draws in iframes. It adds a Notifications
// app to the phone (the PhoneSMS page): an icon on the phone's dock with the unread count, the newest unread notices on
// the phone's home screen, and a list of every notice of this save, newest first, with filters. The plugin hands it the
// list with set(); the player's clicks go back to the plugin as vuplex messages. All text goes on the page with
// textContent, so no notice can run as script or markup.
(function () {
    if (window.__slnotify) return;

    var PREFIX = 'slmod|notifications|';
    var SVG = 'http://www.w3.org/2000/svg';
    var STYLE_ID = 'slnotify-style';
    var CHECK_MS = 500;
    var CHIPS = 3;
    var PEEK = 3;
    var FILTERS = ['all', 'unread', 'popup', 'msg', 'mod'];
    var P = {
        bell: 'M6 9a6 6 0 0 1 12 0c0 5 2 7 2 7H4s2-2 2-7z M10 19.5a2 2 0 0 0 4 0',
        back: 'M15 6l-6 6 6 6',
        chev: 'M9 6l6 6-6 6',
        checks: 'M2 12.5l5 5L18 6.5 M12 16.5l1 1L22 8.5',
        trash: 'M5 7h14 M10 7V4.5h4V7 M7 7l1 13h8l1-13',
        open: 'M14 4h6v6 M20 4l-9 9 M18 14v6H4V6h6',
        star: 'M12 3.5l2.6 5.3 5.9.9-4.3 4.1 1 5.8-5.2-2.7-5.2 2.7 1-5.8-4.3-4.1 5.9-.9z',
        box: 'M4 8l8-4 8 4v8l-8 4-8-4z M4 8l8 4 8-4 M12 12v8',
        shield: 'M12 3l8 4v5c0 4-3.4 7.6-8 9-4.6-1.4-8-5-8-9V7z',
        up: 'M12 19V6 M6 12l6-6 6 6',
        drone: 'M3 6h6 M15 6h6 M6 6v3 M18 6v3 M7 10h10l-1.5 5h-7z M12 15v3 M9.5 20h5',
        unlock: 'M7 11V8a5 5 0 0 1 9.6-2 M5 11h14v9H5z M12 14.5v2',
        msg: 'M4 6h16v10H8l-4 4z',
        warn: 'M12 4l9 16H3z M12 10v4 M12 17v.5',
        plug: 'M9 3v5 M15 3v5 M6 8h12v3a6 6 0 0 1-12 0z M12 17v4'
    };
    // One rule set per kind instead of color-mix, which the game's browser may not have.
    var KINDS = { success: '140,197,106', info: '217,191,138', warning: '255,200,58', error: '224,97,79' };
    var CSS =
        '.app-dock{flex-wrap:wrap;row-gap:12px}' +
        '.app.is-slnotify .app-icon{background:linear-gradient(180deg,#ef7f4a 0%,#9c3f1a 100%);border:1px solid rgba(0,0,0,0.25);' +
        'box-shadow:inset 0 1px 0 rgba(255,215,190,0.5),inset 0 -2px 4px rgba(60,15,0,0.3),0 4px 10px rgba(0,0,0,0.4)}' +
        '.app.is-slnotify .icon-glyph svg{fill:none;stroke:#2a1800;stroke-width:2.4;stroke-linejoin:round;stroke-linecap:round}' +
        '.slnotify-hidden{display:none !important}' +
        'svg.sn-g{width:16px;height:16px;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;flex:none}' +
        '.view.slnotify-view{z-index:2;background:radial-gradient(circle at center bottom,rgba(251,176,52,0.20) 0%,transparent 62%),' +
        'linear-gradient(135deg,rgba(65,52,45,0.96) 0%,rgba(35,28,24,0.98) 100%)}' +
        '.slnotify-view .title{flex:1;min-width:0}' +
        '.sn-new{font-size:10px;font-weight:700;letter-spacing:.5px;color:#2a1f10;background:#FFC83A;border-radius:8px;padding:1px 7px;text-shadow:none;text-transform:none;white-space:nowrap}' +
        '.sn-iconbtn{width:28px;height:28px;display:flex;align-items:center;justify-content:center;border-radius:6px;cursor:pointer;color:#cbbfa8;border:1px solid transparent;position:relative;flex:none}' +
        '.sn-iconbtn:hover{background:rgba(251,176,52,0.08);border-color:rgba(255,200,58,0.45);color:#fbb034}' +
        '.sn-iconbtn.sn-off{opacity:.35;pointer-events:none}' +
        '.sn-iconbtn:hover::after{content:attr(data-tip);position:absolute;top:32px;right:0;white-space:nowrap;font-size:11px;color:#f0e8d8;background:#1e1812;border:1px solid rgba(220,188,120,0.20);border-radius:4px;padding:3px 8px;z-index:20;text-shadow:none}' +
        '.sn-day{font-size:11px;font-weight:700;letter-spacing:1px;color:#fbb034;padding:3px 8px;border-radius:4px;border:1px solid rgba(255,255,255,0.18);background:linear-gradient(135deg,rgba(70,60,55,0.95),rgba(45,38,34,0.95));white-space:nowrap;flex:none}' +
        '.sn-filters{display:flex;gap:6px;padding:10px 14px 8px;flex-shrink:0;border-bottom:1px solid rgba(255,255,255,0.05);flex-wrap:wrap}' +
        '.sn-chip{font-size:12px;padding:4px 10px;border-radius:4px;border:1px solid rgba(220,188,120,0.20);background:rgba(255,240,210,0.05);color:#cbbfa8;cursor:pointer;display:flex;gap:5px;align-items:center;white-space:nowrap}' +
        '.sn-chip:hover{border-color:rgba(255,200,58,0.6);color:#fff}' +
        '.sn-chip.sn-on{color:#FFC83A;background:rgba(255,200,58,0.13);border-color:#FFC83A}' +
        '.sn-chip i{font-style:normal;font-size:10px;color:#7a6e5a}' +
        '.sn-chip.sn-on i{color:#FFC83A;opacity:.8}' +
        '.slnotify-view .list-scroll{padding:4px 0 10px}' +
        '.sn-divider{display:flex;align-items:center;gap:6px;padding:12px 16px 6px;font-size:10px;letter-spacing:2px;color:#a89a82;font-weight:700}' +
        '.sn-divider::after{content:"";flex:1;height:1px;margin-left:4px;background:linear-gradient(90deg,rgba(255,200,58,0.22),transparent)}' +
        '.sn-divider b{color:#fbb034;font-weight:700}' +
        '.slnotify-row{position:relative;display:flex;gap:12px;padding:10px 14px 10px 16px;margin:2px 6px 0;border-radius:6px;cursor:pointer}' +
        '.slnotify-row:hover{background:rgba(251,176,52,0.05);box-shadow:inset 0 0 0 1px rgba(255,200,58,0.28)}' +
        '.slnotify-row.is-unread{background:linear-gradient(90deg,rgba(255,200,58,0.09),rgba(255,200,58,0.02) 60%,transparent)}' +
        '.slnotify-row.is-unread::before{content:"";position:absolute;left:4px;top:50%;width:6px;height:6px;margin-top:-3px;border-radius:50%;background:#FFC83A;box-shadow:0 0 6px rgba(255,200,58,0.35)}' +
        '.slnotify-row.is-open{background:rgba(70,58,44,0.6);box-shadow:inset 0 0 0 1px rgba(255,200,58,0.35)}' +
        '.sn-tile{position:relative;width:40px;height:40px;flex:none;border-radius:8px;display:flex;align-items:center;justify-content:center;box-shadow:inset 0 1px 2px rgba(0,0,0,0.4)}' +
        '.sn-tile svg.sn-g{width:20px;height:20px;stroke-width:1.9}' +
        '.sn-tile img{width:34px;height:34px;object-fit:contain;filter:drop-shadow(0 1px 2px rgba(0,0,0,.5))}' +
        '.sn-kb{position:absolute;right:-4px;bottom:-4px;width:17px;height:17px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:#2a221b}' +
        '.sn-kb svg.sn-g{width:10px;height:10px;stroke-width:2.6}' +
        '.sn-main{flex:1;min-width:0}' +
        '.sn-top{display:flex;align-items:baseline;gap:6px}' +
        '.sn-title{font-size:13.5px;font-weight:600;color:#cbbfa8;letter-spacing:.3px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;min-width:0}' +
        '.is-unread .sn-title{color:#fff}' +
        '.sn-count{font-size:10px;font-weight:700;color:#2a1f10;background:#FFC83A;border-radius:7px;padding:0 5px;line-height:14px;flex:none;text-shadow:none}' +
        '.sn-time{margin-left:auto;font-size:11px;color:#7a6e5a;white-space:nowrap;flex:none}' +
        '.is-unread .sn-time{color:#fbb034}' +
        '.sn-src{display:inline-block;font-size:9.5px;padding:0 5px;line-height:15px;border-radius:3px;background:rgba(255,200,58,0.10);color:#FFC83A;border:1px solid rgba(255,200,58,0.35);letter-spacing:.4px;margin-right:6px;vertical-align:1px}' +
        '.sn-body{font-size:12px;line-height:1.45;color:#a89a82;margin-top:3px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}' +
        '.is-open .sn-body{display:block;color:#cbbfa8}' +
        '.sn-stats{display:flex;flex-wrap:wrap;gap:4px 10px;margin-top:4px;font-size:11.5px;color:#a89a82}' +
        '.sn-stats b{color:#f0e8d8;font-weight:600;margin-left:4px}' +
        '.sn-stats .sn-up b{color:#8cc56a}' +
        '.sn-items{display:flex;flex-wrap:wrap;gap:4px;margin-top:5px}' +
        '.sn-item{display:inline-flex;align-items:center;gap:3px;padding:1px 6px 1px 2px;border-radius:4px;background:rgba(20,14,8,0.45);border:1px solid rgba(220,188,120,0.11);font-size:11px;color:#cbbfa8}' +
        '.sn-item img{width:18px;height:18px;object-fit:contain}' +
        '.sn-item b{color:#f0e8d8;font-weight:600;margin-left:3px}' +
        '.sn-item.sn-more{padding:1px 6px;color:#7a6e5a}' +
        '.sn-acts{display:flex;gap:6px;margin-top:8px;align-items:center}' +
        '.sn-hint{font-size:10.5px;color:#7a6e5a}' +
        '.sn-btn{font-size:12px;padding:4px 12px;border-radius:4px;cursor:pointer;color:#f0e8d8;border:1px solid rgba(220,188,120,0.20);background:linear-gradient(180deg,rgba(92,78,60,0.9),rgba(66,55,42,0.92));display:inline-flex;gap:5px;align-items:center;white-space:nowrap}' +
        '.sn-btn:hover{border-color:#FFC83A}' +
        '.sn-btn.sn-hot{color:#2a1f10;font-weight:700;border-color:#ffd36a;background:linear-gradient(180deg,#ffd25a,#e8a91c);text-shadow:none}' +
        '.sn-btn svg.sn-g{width:12px;height:12px}' +
        '.sn-chev{align-self:center;color:#7a6e5a;flex:none;margin-left:-4px}' +
        '.slnotify-row:hover .sn-chev{color:#fbb034}' +
        '.is-open .sn-chev{transform:rotate(90deg);color:#fbb034}' +
        '.sn-foot{flex-shrink:0;padding:8px 16px 10px;font-size:10.5px;color:#7a6e5a;border-top:1px solid rgba(255,255,255,0.05);display:flex;gap:10px;align-items:center}' +
        '.sn-kept{flex:1}' +
        '.sn-clear{display:inline-flex;gap:4px;align-items:center;color:#a89a82;cursor:pointer}' +
        '.sn-clear svg.sn-g{width:12px;height:12px}' +
        '.sn-clear:hover{color:#e0614f}' +
        '.sn-ask{color:#f0e8d8}' +
        '.sn-foot .sn-btn{font-size:11px;padding:2px 10px}' +
        '.sn-foot .sn-yes{color:#fff;border-color:rgba(224,97,79,0.7);background:linear-gradient(180deg,rgba(170,70,52,0.95),rgba(120,44,32,0.95))}' +
        '.sn-empty{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;padding:40px 50px;text-align:center}' +
        '.sn-bell{width:74px;height:74px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:radial-gradient(circle,rgba(255,200,58,0.12),transparent 70%);color:#7a6e5a}' +
        '.sn-bell svg.sn-g{width:44px;height:44px;stroke-width:1.4}' +
        '.sn-et{font-size:14px;color:#cbbfa8;font-weight:600;letter-spacing:.5px}' +
        '.sn-eh{font-size:11.5px;color:#7a6e5a;line-height:1.55}' +
        '.app-grid.slnotify-peeking{flex-direction:column;align-items:stretch;justify-content:flex-start;flex-wrap:nowrap;gap:6px;padding:18px 16px 12px;min-height:0}' +
        '.sn-peek{display:flex;flex-direction:column;gap:6px}' +
        '.sn-peek-head{display:flex;align-items:center;font-size:10px;letter-spacing:2px;color:#a89a82;font-weight:700;padding:0 2px}' +
        '.sn-peek-head span{flex:1}' +
        '.sn-seeall{letter-spacing:.5px;font-weight:600;color:#fbb034;cursor:pointer}' +
        '.sn-card{display:flex;gap:10px;align-items:center;padding:8px 10px;border-radius:8px;cursor:pointer;background:linear-gradient(180deg,rgba(74,64,56,0.55),rgba(40,33,27,0.5));border:1px solid rgba(255,255,255,0.10);box-shadow:0 4px 12px rgba(0,0,0,0.25)}' +
        '.sn-card:hover{border-color:rgba(255,200,58,0.45)}' +
        '.sn-card .sn-tile{width:32px;height:32px;border-radius:7px}' +
        '.sn-card .sn-tile img{width:26px;height:26px}' +
        '.sn-card .sn-tile svg.sn-g{width:16px;height:16px}' +
        '.sn-card .sn-kb{display:none}' +
        '.sn-card .sn-title{color:#fff;font-size:12.5px}' +
        '.sn-card .sn-body{white-space:nowrap;text-overflow:ellipsis;display:block;font-size:11px;margin-top:1px}' +
        '.sn-card .sn-time{align-self:flex-start}' +
        kindRules();

    var view = { label: 'Notifications', unread: 0, mark: 0, kept: 0, cap: 0, text: {}, rows: [] };
    var revision = 0;
    var reported = {};
    // What the player has open in the list; it survives the plugin's redraws and the phone's.
    var ui = { filter: 'all', open: null, asking: false };

    function kindRules() {
        var css = '';

        for (var kind in KINDS) {
            var rgb = KINDS[kind];
            var tile = '.sn-k-' + kind;

            css += tile + '{color:rgb(' + rgb + ');border:1px solid rgba(' + rgb + ',0.45);' +
                'background:linear-gradient(135deg,rgba(' + rgb + ',0.22),rgba(' + rgb + ',0.05))}' +
                tile + ' .sn-kb{border:1.5px solid rgb(' + rgb + ');color:rgb(' + rgb + ')}';
        }

        return css;
    }

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

    function word(key) {
        var text = view.text && view.text[key];

        return text === undefined || text === null ? key : String(text);
    }

    // Icon paths come from the game's WebUI folder; the phone is a page two folders down, so they are made absolute
    // from this root page's address.
    function fromRoot(path) {
        var href = String((window.location && window.location.href) || '');

        if (/^[a-z]+:|^\//i.test(path) || href.indexOf('/') < 0) return path;

        return href.slice(0, href.lastIndexOf('/') + 1) + path;
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

    function icon(doc, path, className) {
        var svg = doc.createElementNS ? doc.createElementNS(SVG, 'svg') : doc.createElement('svg');
        var line = doc.createElementNS ? doc.createElementNS(SVG, 'path') : doc.createElement('path');

        svg.setAttribute('viewBox', '0 0 24 24');
        if (className !== '') svg.setAttribute('class', className || 'sn-g');
        line.setAttribute('d', path);
        line.setAttribute('stroke-linecap', 'round');
        line.setAttribute('stroke-linejoin', 'round');
        svg.appendChild(line);

        return svg;
    }

    function picture(doc, path, className) {
        var img = element(doc, 'img', className);

        img.setAttribute('src', fromRoot(path));
        img.setAttribute('alt', '');

        return img;
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

    // A click on a control inside a row must not also open or close the row.
    function onClick(target, handler) {
        listen(target, 'click', function (event) {
            if (event && event.stopPropagation) event.stopPropagation();

            handler(event);
        });
    }

    function clear(node) {
        while (node.firstChild) node.removeChild(node.firstChild);
    }

    function show(node, isShown) {
        if (!node) return;

        if (isShown) node.classList.remove('slnotify-hidden');
        else node.classList.add('slnotify-hidden');
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
            dock.appendChild(buildIcon(doc));
            isChanged = true;
        }

        var grid = doc.querySelector('.view-home .app-grid');

        if (grid && !grid.querySelector('.sn-peek')) {
            var peek = element(doc, 'div', 'sn-peek slnotify-hidden');

            peek.__slnotifyRevision = -1;
            grid.appendChild(peek);
            isChanged = true;
        }

        var panel = doc.querySelector('.sms-panel');

        if (panel && !panel.querySelector('.slnotify-view')) {
            panel.appendChild(buildView(doc));
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

    function buildIcon(doc) {
        var app = element(doc, 'div', 'app is-slnotify');
        var badge = element(doc, 'div', 'unread-dot slnotify-badge slnotify-hidden');
        var tile = element(doc, 'div', 'app-icon');
        var glyph = element(doc, 'div', 'icon-glyph');

        glyph.appendChild(icon(doc, P.bell, ''));
        tile.appendChild(glyph);
        app.appendChild(badge);
        app.appendChild(tile);
        app.appendChild(element(doc, 'div', 'app-label', view.label));
        onClick(app, function () { openList(doc); });

        return app;
    }

    function buildView(doc) {
        var panelView = element(doc, 'div', 'view slnotify-view');
        var head = element(doc, 'div', 'panel-head');
        var back = element(doc, 'div', 'back');
        var title = element(doc, 'div', 'title');
        var readAll = element(doc, 'div', 'sn-iconbtn sn-readall');

        back.appendChild(icon(doc, P.back, ''));
        listen(back, 'click', function () { closeList(doc); });
        title.appendChild(element(doc, 'span', 't-name', view.label));
        title.appendChild(element(doc, 'span', 'sn-new slnotify-hidden'));
        readAll.appendChild(icon(doc, P.checks));
        onClick(readAll, function () { markAllRead(); });
        head.appendChild(back);
        head.appendChild(title);
        head.appendChild(readAll);
        head.appendChild(element(doc, 'div', 'sn-day slnotify-hidden'));
        panelView.appendChild(head);
        panelView.appendChild(element(doc, 'div', 'sn-filters'));
        panelView.appendChild(element(doc, 'div', 'list-scroll sn-scroll'));
        panelView.appendChild(element(doc, 'div', 'sn-foot'));
        panelView.__slnotifyRevision = -1;

        return panelView;
    }

    // Opening the list reads nothing: a notice counts as read once its row is opened or the player marks all read.
    function openList(doc, seq) {
        var panelView = doc.querySelector('.slnotify-view');

        if (!panelView) return;

        if (seq !== undefined) {
            ui.filter = 'all';
            ui.asking = false;
            expand(seq, true);
        }

        panelView.classList.add('is-active');
    }

    function closeList(doc) {
        var panelView = doc.querySelector('.slnotify-view');

        if (panelView) panelView.classList.remove('is-active');
    }

    // The plugin's next list says the same; until it comes, the page shows the change at once.
    function changed() {
        revision++;
        check();
    }

    function findRow(seq) {
        for (var i = 0; i < view.rows.length; i++) {
            if (view.rows[i].seq === seq) return view.rows[i];
        }

        return null;
    }

    function markRead(row) {
        if (!row || !row.unread) return;

        row.unread = false;
        view.unread = Math.max(0, (view.unread || 0) - 1);
        post('read|' + row.seq);
    }

    function expand(seq, isOnly) {
        var row = findRow(seq);

        ui.open = ui.open === seq && !isOnly ? null : seq;
        markRead(row);
        changed();
    }

    function markAllRead() {
        if (!(view.unread > 0)) return;

        for (var i = 0; i < view.rows.length; i++) view.rows[i].unread = false;

        view.unread = 0;
        post('seen|' + view.mark);
        changed();
    }

    function clearList() {
        post('clear|' + view.mark);
        view.rows = [];
        view.unread = 0;
        view.kept = 0;
        ui.open = null;
        ui.asking = false;
        changed();
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

    function dayText(doc, day) {
        return dayLabel(doc).replace('{0}', String(day));
    }

    function hourText(row) {
        if (typeof row.hour !== 'number') return '';

        return (row.hour < 10 ? '0' : '') + row.hour + ':00';
    }

    function isShown(row, filter) {
        if (filter === 'all') return true;

        return filter === 'unread' ? !!row.unread : row.cat === filter;
    }

    function tile(doc, row) {
        var made = element(doc, 'div', 'sn-tile sn-k-' + (KINDS[row.kind] ? row.kind : 'info'));
        var glyph = P[row.glyph] || P.msg;

        if (row.icon) {
            var badge = element(doc, 'span', 'sn-kb');

            made.appendChild(picture(doc, row.icon));
            badge.appendChild(icon(doc, glyph));
            made.appendChild(badge);
        } else {
            made.appendChild(icon(doc, glyph));
        }

        return made;
    }

    function topLine(doc, row) {
        var top = element(doc, 'div', 'sn-top');

        top.appendChild(element(doc, 'span', 'sn-title', row.title));

        if (row.count > 1) top.appendChild(element(doc, 'span', 'sn-count', 'x' + row.count));

        top.appendChild(element(doc, 'span', 'sn-time', hourText(row)));

        return top;
    }

    function bodyLine(doc, row) {
        if (!row.body && row.game) return null;

        var body = element(doc, 'div', 'sn-body');

        if (!row.game) body.appendChild(element(doc, 'span', 'sn-src', row.source));
        if (row.body) body.appendChild(element(doc, 'span', 'sn-text', row.body));

        return body;
    }

    function itemChip(doc, item) {
        var chip = element(doc, 'span', 'sn-item');

        if (item.icon) chip.appendChild(picture(doc, item.icon));

        chip.appendChild(element(doc, 'span', 'sn-name', item.name));
        chip.appendChild(element(doc, 'b', '', 'x' + item.count));

        return chip;
    }

    function buildRow(doc, row) {
        var isOpen = ui.open === row.seq;
        var line = element(doc, 'div', 'list-row slnotify-row');
        var main = element(doc, 'div', 'sn-main');
        var chev = element(doc, 'div', 'sn-chev');
        var body = bodyLine(doc, row);

        line.setAttribute('data-seq', String(row.seq));

        if (row.unread) line.classList.add('is-unread');
        if (isOpen) line.classList.add('is-open');

        main.appendChild(topLine(doc, row));

        if (body) main.appendChild(body);

        if (row.stats && row.stats.length) {
            var stats = element(doc, 'div', 'sn-stats');

            for (var s = 0; s < row.stats.length; s++) {
                var stat = element(doc, 'span', row.stats[s].up ? 'sn-stat sn-up' : 'sn-stat', row.stats[s].label);

                stat.appendChild(element(doc, 'b', '', row.stats[s].value));
                stats.appendChild(stat);
            }

            main.appendChild(stats);
        }

        if (row.items && row.items.length) {
            var items = element(doc, 'div', 'sn-items');
            var shown = isOpen ? row.items.length : Math.min(CHIPS, row.items.length);

            for (var i = 0; i < shown; i++) items.appendChild(itemChip(doc, row.items[i]));

            if (shown < row.items.length) items.appendChild(element(doc, 'span', 'sn-item sn-more', word('more').replace('{0}', String(row.items.length - shown))));

            main.appendChild(items);
        }

        if (isOpen) main.appendChild(actions(doc, row));

        chev.appendChild(icon(doc, P.chev));
        line.appendChild(tile(doc, row));
        line.appendChild(main);
        line.appendChild(chev);
        listen(line, 'click', function () { expand(row.seq, false); });

        return line;
    }

    function actions(doc, row) {
        var acts = element(doc, 'div', 'sn-acts');

        if (row.popup || row.click) {
            var open = element(doc, 'div', row.popup ? 'sn-btn sn-hot sn-open' : 'sn-btn sn-open');

            open.appendChild(icon(doc, P.open));
            open.appendChild(element(doc, 'span', '', word(row.popup ? 'open' : 'openPlain')));
            onClick(open, function () { openRow(doc, row.seq); });
            acts.appendChild(open);

            if (row.popup) acts.appendChild(element(doc, 'span', 'sn-hint', word('hintPopup')));
        } else {
            acts.appendChild(element(doc, 'span', 'sn-hint', word('hintNone')));
        }

        return acts;
    }

    // A row opens its popup again: the list and the phone close first, so the popup is not drawn under the phone.
    function openRow(doc, seq) {
        closeList(doc);

        var appRoot = doc.querySelector('.app-root');

        if (appRoot && appRoot.click) appRoot.click();

        post('open|' + seq);
    }

    function emptyBlock(doc, title, hint) {
        var empty = element(doc, 'div', 'sn-empty');
        var bell = element(doc, 'div', 'sn-bell');

        bell.appendChild(icon(doc, P.bell));
        empty.appendChild(bell);
        empty.appendChild(element(doc, 'div', 'sn-et', title));

        if (hint) empty.appendChild(element(doc, 'div', 'sn-eh', hint));

        return empty;
    }

    function drawHead(doc, panelView) {
        var title = panelView.querySelector('.t-name');
        var fresh = panelView.querySelector('.sn-new');
        var readAll = panelView.querySelector('.sn-readall');
        var day = panelView.querySelector('.sn-day');

        if (title) title.textContent = view.label;

        if (fresh) {
            fresh.textContent = view.unread + ' ' + word('new');
            show(fresh, view.unread > 0);
        }

        if (readAll) {
            readAll.setAttribute('data-tip', word('markRead'));
            if (view.unread > 0) readAll.classList.remove('sn-off');
            else readAll.classList.add('sn-off');
        }

        if (day) {
            day.textContent = typeof view.today === 'number' ? dayText(doc, view.today) : '';
            show(day, typeof view.today === 'number');
        }
    }

    function drawFilters(doc, panelView) {
        var filters = panelView.querySelector('.sn-filters');

        if (!filters) return;

        clear(filters);
        show(filters, view.rows.length > 0);

        var counts = { all: view.rows.length, unread: 0, popup: 0, msg: 0, mod: 0 };

        for (var i = 0; i < view.rows.length; i++) {
            if (view.rows[i].unread) counts.unread++;
            if (counts[view.rows[i].cat] !== undefined) counts[view.rows[i].cat]++;
        }

        FILTERS.forEach(function (filter) {
            var chip = element(doc, 'div', 'sn-chip sn-f-' + filter + (ui.filter === filter ? ' sn-on' : ''), word(filter));

            if (counts[filter]) chip.appendChild(element(doc, 'i', '', String(counts[filter])));

            onClick(chip, function () {
                ui.filter = filter;
                changed();
            });
            filters.appendChild(chip);
        });
    }

    function drawRows(doc, panelView) {
        var scroll = panelView.querySelector('.sn-scroll');

        if (!scroll) return;

        clear(scroll);

        if (view.rows.length === 0) {
            scroll.appendChild(emptyBlock(doc, word('emptyTitle'), word('emptyHint')));

            return;
        }

        var day = null;
        var any = false;

        for (var i = 0; i < view.rows.length; i++) {
            var row = view.rows[i];

            if (!isShown(row, ui.filter)) continue;

            any = true;

            if (typeof row.day === 'number' && row.day !== day) {
                var divider = element(doc, 'div', 'sn-divider');

                day = row.day;

                if (day === view.today) divider.appendChild(element(doc, 'b', '', word('today')));

                divider.appendChild(element(doc, 'span', '', dayText(doc, day)));
                scroll.appendChild(divider);
            }

            scroll.appendChild(buildRow(doc, row));
        }

        if (!any) scroll.appendChild(emptyBlock(doc, word('emptyFilter'), ''));
    }

    function drawFoot(doc, panelView) {
        var foot = panelView.querySelector('.sn-foot');

        if (!foot) return;

        clear(foot);
        show(foot, view.rows.length > 0);

        if (ui.asking) {
            var yes = element(doc, 'div', 'sn-btn sn-yes', word('yes'));
            var no = element(doc, 'div', 'sn-btn sn-no', word('no'));

            onClick(yes, function () { clearList(); });
            onClick(no, function () {
                ui.asking = false;
                changed();
            });
            foot.appendChild(element(doc, 'span', 'sn-kept sn-ask', word('clearAsk')));
            foot.appendChild(no);
            foot.appendChild(yes);

            return;
        }

        var wipe = element(doc, 'div', 'sn-clear');

        wipe.appendChild(icon(doc, P.trash));
        wipe.appendChild(element(doc, 'span', '', word('clear')));
        onClick(wipe, function () {
            ui.asking = true;
            changed();
        });
        foot.appendChild(element(doc, 'span', 'sn-kept', word('kept').replace('{0}', String(view.kept || 0)).replace('{1}', String(view.cap || 0))));
        foot.appendChild(wipe);
    }

    // The newest unread notices on the phone's home screen, in the space the game leaves empty above the dock.
    function drawPeek(doc) {
        var grid = doc.querySelector('.view-home .app-grid');
        var peek = grid && grid.querySelector('.sn-peek');

        if (!peek || peek.__slnotifyRevision === revision) return;

        peek.__slnotifyRevision = revision;
        clear(peek);

        var unread = [];

        for (var i = 0; i < view.rows.length && unread.length < PEEK; i++) {
            if (view.rows[i].unread) unread.push(view.rows[i]);
        }

        show(peek, unread.length > 0);

        if (unread.length > 0) grid.classList.add('slnotify-peeking');
        else grid.classList.remove('slnotify-peeking');

        if (unread.length === 0) return;

        var head = element(doc, 'div', 'sn-peek-head');
        var seeAll = element(doc, 'span', 'sn-seeall', word('seeAll'));

        head.appendChild(element(doc, 'span', '', word('recent')));
        onClick(seeAll, function () { openList(doc); });
        head.appendChild(seeAll);
        peek.appendChild(head);

        unread.forEach(function (row) {
            var card = element(doc, 'div', 'sn-card');
            var main = element(doc, 'div', 'sn-main');
            var body = bodyLine(doc, row);

            card.setAttribute('data-seq', String(row.seq));
            main.appendChild(topLine(doc, row));

            if (body) main.appendChild(body);

            card.appendChild(tile(doc, row));
            card.appendChild(main);
            onClick(card, function () { openList(doc, row.seq); });
            peek.appendChild(card);
        });
    }

    function drawBadge(doc) {
        var badge = doc.querySelector('.is-slnotify .unread-dot');
        var label = doc.querySelector('.is-slnotify .app-label');

        if (label) label.textContent = view.label;
        if (!badge) return;

        badge.textContent = view.unread > 99 ? '99+' : String(view.unread || 0);
        show(badge, view.unread > 0);
    }

    function draw(doc) {
        drawBadge(doc);
        drawPeek(doc);

        var panelView = doc.querySelector('.slnotify-view');

        if (!panelView || panelView.__slnotifyRevision === revision) return;

        panelView.__slnotifyRevision = revision;
        drawHead(doc, panelView);
        drawFilters(doc, panelView);
        drawRows(doc, panelView);
        drawFoot(doc, panelView);
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

            if (ui.open !== null && !findRow(ui.open)) ui.open = null;
            if (view.rows.length === 0) ui.asking = false;

            changed();
        },
        check: check
    };

    setInterval(check, CHECK_MS);
    check();
})();
