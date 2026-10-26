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
    var PHONE_PAGE = /PhoneSMS\.html/i;
    var CHASE_FRAMES = 120;
    var CHIPS = 3;
    var PEEK = 3;
    // The list draws this many rows when it opens and this many more each time the player scrolls near its end, so
    // opening it builds a screenful, not every kept notice.
    var ROWS_AT_ONCE = 40;
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
        '.sn-more-rows{height:1px}' +
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
    var ui = { filter: 'all', open: null, asking: false, limit: ROWS_AT_ONCE };
    // The rows of the last list by number: the plugin sends a row the page already has as its number alone.
    var known = {};
    // Every phone page the app is on, with the app's own parts in it, so a redraw finds them without searching.
    var phones = [];

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

    // A frame from another origin throws on reading its location; it is not the phone, so it is passed over.
    function phoneIn(frame) {
        var win = frame.contentWindow;

        if (!win) return null;

        try {
            if (PHONE_PAGE.test(String(win.location)) && win.document) return { frame: frame, win: win, doc: win.document };
        } catch (e) {
            // Not ours to read.
        }

        return null;
    }

    // Everything the app puts on a page is marked, so the page's watcher can tell the app's changes from the game's.
    function element(doc, tag, className, text) {
        var made = doc.createElement(tag);

        made.__sn = 1;

        if (className) made.className = className;
        if (text !== undefined) made.textContent = text;

        return made;
    }

    function icon(doc, path, className) {
        var svg = doc.createElementNS ? doc.createElementNS(SVG, 'svg') : doc.createElement('svg');
        var line = doc.createElementNS ? doc.createElementNS(SVG, 'path') : doc.createElement('path');

        svg.__sn = 1;
        line.__sn = 1;
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

    // The writes below change the page only when it does not already say so: every write is work for the browser, and
    // a change to the phone's page wakes its watcher.
    function setText(node, text) {
        if (node.textContent !== text) node.textContent = text;
    }

    function setClass(node, name, isOn) {
        if (node.classList.contains(name) === isOn) return;

        if (isOn) node.classList.add(name);
        else node.classList.remove(name);
    }

    function show(node, isShown) {
        if (node) setClass(node, 'slnotify-hidden', !isShown);
    }

    // Still on its page. A part the phone's own redraw took away is no longer.
    function isLive(node) {
        return !!node && node.isConnected === true;
    }

    // The app's part under a container of the game's: the one already there, or a new one. A part this page did not
    // make for this phone is replaced.
    function place(phone, containerSelector, ownSelector, build) {
        var container = phone.doc.querySelector(containerSelector);

        if (!container) return null;

        var found = container.querySelector(ownSelector);

        if (found && found.__snRefs && found.__snRefs.phone === phone) return found;

        if (found && found.parentElement) found.parentElement.removeChild(found);

        return container.appendChild(build(phone));
    }

    // Puts back whatever of the app the phone's own redraws took away. Only a missing part is searched for.
    function ensure(phone) {
        var doc = phone.doc;

        if (!isLive(phone.style)) {
            phone.style = doc.getElementById(STYLE_ID);

            if (!phone.style && (doc.head || doc.body)) {
                phone.style = element(doc, 'style');
                phone.style.id = STYLE_ID;
                phone.style.textContent = CSS;
                (doc.head || doc.body).appendChild(phone.style);
            }
        }

        if (!isLive(phone.icon)) phone.icon = place(phone, '.app-dock', '.is-slnotify', buildIcon);
        if (!isLive(phone.peek)) phone.peek = place(phone, '.view-home .app-grid', '.sn-peek', buildPeek);
        if (!isLive(phone.view)) phone.view = place(phone, '.sms-panel', '.slnotify-view', buildView);

        if (!isLive(phone.appRoot)) {
            phone.appRoot = doc.querySelector('.app-root');

            if (phone.appRoot && !phone.appRoot.__slnotifyListening) {
                phone.appRoot.__slnotifyListening = true;
                // A click outside the phone's panel closes the phone; the list must not be open when it comes back.
                listen(phone.appRoot, 'click', function () { closeList(phone); });
            }
        }
    }

    function buildIcon(phone) {
        var doc = phone.doc;
        var app = element(doc, 'div', 'app is-slnotify');
        var badge = element(doc, 'div', 'unread-dot slnotify-badge slnotify-hidden');
        var tile = element(doc, 'div', 'app-icon');
        var glyph = element(doc, 'div', 'icon-glyph');
        var label = element(doc, 'div', 'app-label', view.label);

        glyph.appendChild(icon(doc, P.bell, ''));
        tile.appendChild(glyph);
        app.appendChild(badge);
        app.appendChild(tile);
        app.appendChild(label);
        app.__snRefs = { phone: phone, badge: badge, label: label };
        onClick(app, function () { openList(phone); });

        return app;
    }

    function buildPeek(phone) {
        var peek = element(phone.doc, 'div', 'sn-peek slnotify-hidden');

        peek.__snRefs = { phone: phone, shown: null, text: null };

        return peek;
    }

    function buildView(phone) {
        var doc = phone.doc;
        var panelView = element(doc, 'div', 'view slnotify-view');
        var head = element(doc, 'div', 'panel-head');
        var back = element(doc, 'div', 'back');
        var title = element(doc, 'div', 'title');
        var name = element(doc, 'span', 't-name', view.label);
        var fresh = element(doc, 'span', 'sn-new slnotify-hidden');
        var readAll = element(doc, 'div', 'sn-iconbtn sn-readall');
        var day = element(doc, 'div', 'sn-day slnotify-hidden');
        var filters = element(doc, 'div', 'sn-filters');
        var scroll = element(doc, 'div', 'list-scroll sn-scroll');
        var foot = element(doc, 'div', 'sn-foot');
        var chips = {};

        back.appendChild(icon(doc, P.back, ''));
        listen(back, 'click', function () { closeList(phone); });
        title.appendChild(name);
        title.appendChild(fresh);
        readAll.appendChild(icon(doc, P.checks));
        onClick(readAll, function () { markAllRead(); });
        head.appendChild(back);
        head.appendChild(title);
        head.appendChild(readAll);
        head.appendChild(day);

        // The filter chips are made once; each draw changes only their words, counts and which is on.
        FILTERS.forEach(function (filter) {
            var chip = element(doc, 'div', 'sn-chip sn-f-' + filter);
            var label = element(doc, 'span', '', '');
            var count = element(doc, 'i', 'slnotify-hidden', '');

            chip.appendChild(label);
            chip.appendChild(count);
            onClick(chip, function () {
                ui.filter = filter;
                ui.limit = ROWS_AT_ONCE;
                changed();
            });
            filters.appendChild(chip);
            chips[filter] = { chip: chip, label: label, count: count };
        });

        // One listener for every row: a row opens or closes when a click inside it reaches the list. The buttons in an
        // open row stop their own clicks.
        listen(scroll, 'click', function (event) {
            var line = rowOf(event && event.target, scroll);

            if (line) expand(line.__snRow.seq, false);
        });
        panelView.appendChild(head);
        panelView.appendChild(filters);
        panelView.appendChild(scroll);
        panelView.appendChild(foot);
        panelView.__snRefs = {
            phone: phone, name: name, fresh: fresh, readAll: readAll, day: day, filters: filters, chips: chips, scroll: scroll, foot: foot,
            footKey: null, footText: null, nodes: {}, revision: -1, rowsRevision: -1, rowsLimit: -1, reveal: null, revealLimit: -1, revealNode: null
        };

        return panelView;
    }

    function rowOf(node, scroll) {
        for (; node && node !== scroll; node = node.parentElement) {
            if (node.classList && node.classList.contains('slnotify-row')) return node;
        }

        return null;
    }

    // Watches the end of the drawn rows inside the list; when it comes near the visible part, the next rows are drawn.
    // Without the browser's observer every row is drawn at once, as before.
    function revealer(phone, refs) {
        if (refs.reveal) return refs.reveal;

        var Observer = phone.win.IntersectionObserver || window.IntersectionObserver;

        if (!Observer) return null;

        refs.reveal = new Observer(function (entries) {
            try {
                for (var i = 0; i < entries.length; i++) {
                    if (!entries[i].isIntersecting) continue;

                    ui.limit = refs.revealLimit + ROWS_AT_ONCE;
                    schedule(phone);

                    return;
                }
            } catch (e) {
                report('list scroll failed: ' + e);
            }
        }, { root: refs.scroll, rootMargin: '0px 0px 300px 0px' });

        return refs.reveal;
    }

    function isListOpen(phone) {
        return !!phone.view && phone.view.classList.contains('is-active');
    }

    // Opening the list reads nothing: a notice counts as read once its row is opened or the player marks all read. Its
    // rows are drawn only while it is open, so they are drawn here, before it shows.
    function openList(phone, seq) {
        if (!phone.view) return;

        if (seq !== undefined) {
            ui.filter = 'all';
            ui.asking = false;
            expand(seq, true);
        }

        ui.limit = ROWS_AT_ONCE;
        setClass(phone.view, 'is-active', true);
        drawNow(phone);
    }

    function closeList(phone) {
        if (phone.view) setClass(phone.view, 'is-active', false);
    }

    // The plugin's next list says the same; until it comes, the page shows the change by the next frame.
    function changed() {
        revision++;

        for (var i = phones.length - 1; i >= 0; i--) {
            if (isCurrent(phones[i])) schedule(phones[i]);
            else drop(i);
        }
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

    function dayText(label, day) {
        return label.replace('{0}', String(day));
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

    // A row's element remembers what it was drawn from, so the next draw keeps it when nothing it shows has changed.
    function buildRow(phone, row, isOpen) {
        var doc = phone.doc;
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

        if (isOpen) main.appendChild(actions(phone, row));

        chev.appendChild(icon(doc, P.chev));
        line.appendChild(tile(doc, row));
        line.appendChild(main);
        line.appendChild(chev);
        line.__snRow = row;
        line.__snOpen = isOpen;
        line.__snText = view.text;

        return line;
    }

    function actions(phone, row) {
        var doc = phone.doc;
        var acts = element(doc, 'div', 'sn-acts');

        if (row.popup || row.click) {
            var open = element(doc, 'div', row.popup ? 'sn-btn sn-hot sn-open' : 'sn-btn sn-open');

            open.appendChild(icon(doc, P.open));
            open.appendChild(element(doc, 'span', '', word(row.popup ? 'open' : 'openPlain')));
            onClick(open, function () { openRow(phone, row.seq); });
            acts.appendChild(open);

            if (row.popup) acts.appendChild(element(doc, 'span', 'sn-hint', word('hintPopup')));
        } else {
            acts.appendChild(element(doc, 'span', 'sn-hint', word('hintNone')));
        }

        return acts;
    }

    // A row opens its popup again: the list and the phone close first, so the popup is not drawn under the phone.
    function openRow(phone, seq) {
        closeList(phone);

        var appRoot = isLive(phone.appRoot) ? phone.appRoot : phone.doc.querySelector('.app-root');

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

        empty.__snText = view.text;

        return empty;
    }

    function drawHead(refs, label) {
        var tip = word('markRead');
        var hasDay = typeof view.today === 'number';

        setText(refs.name, view.label);
        setText(refs.fresh, view.unread + ' ' + word('new'));
        show(refs.fresh, view.unread > 0);

        if (refs.readAll.getAttribute('data-tip') !== tip) refs.readAll.setAttribute('data-tip', tip);

        setClass(refs.readAll, 'sn-off', !(view.unread > 0));
        setText(refs.day, hasDay ? dayText(label, view.today) : '');
        show(refs.day, hasDay);
    }

    function drawFilters(refs) {
        show(refs.filters, view.rows.length > 0);

        var counts = { all: view.rows.length, unread: 0, popup: 0, msg: 0, mod: 0 };

        for (var i = 0; i < view.rows.length; i++) {
            if (view.rows[i].unread) counts.unread++;
            if (counts[view.rows[i].cat] !== undefined) counts[view.rows[i].cat]++;
        }

        for (var f = 0; f < FILTERS.length; f++) {
            var filter = FILTERS[f];
            var chip = refs.chips[filter];

            setClass(chip.chip, 'sn-on', ui.filter === filter);
            setText(chip.label, word(filter));
            setText(chip.count, counts[filter] ? String(counts[filter]) : '');
            show(chip.count, counts[filter] > 0);
        }
    }

    // A node kept from the last draw, under its key, when it still shows the same; else a new one.
    function keep(old, nodes, key, isSame, make) {
        var node = old[key];

        if (!node || !isSame(node)) node = make();

        nodes[key] = node;

        return node;
    }

    function sameText(node) {
        return node.__snText === view.text;
    }

    // Draws the list by keeping every row, day line and empty notice whose content is unchanged and moving them into
    // order: a new notice adds one row, a read one changes one class, and nothing else on the page is touched.
    function drawRows(phone, refs, label) {
        var doc = phone.doc;
        var scroll = refs.scroll;
        var old = refs.nodes;
        var pendingReveal = null;
        var nodes = {};
        var wanted = [];

        if (view.rows.length === 0) {
            wanted.push(keep(old, nodes, 'empty', sameText, function () { return emptyBlock(doc, word('emptyTitle'), word('emptyHint')); }));
        } else {
            var day = null;
            var days = {};
            var reveal = revealer(phone, refs);
            var limit = reveal ? rowLimit() : view.rows.length;
            var drawn = 0;
            var isCut = false;

            for (var i = 0; i < view.rows.length; i++) {
                var row = view.rows[i];

                if (!isShown(row, ui.filter)) continue;

                if (drawn >= limit) {
                    isCut = true;
                    break;
                }

                drawn++;

                if (typeof row.day === 'number' && row.day !== day) {
                    day = row.day;

                    var dayKey = 'd' + day;
                    var text = (day === view.today ? word('today') + ' ' : '') + dayText(label, day);

                    days[dayKey] = (days[dayKey] || 0) + 1;
                    wanted.push(keep(old, nodes, dayKey + '#' + days[dayKey], function (node) { return node.__snDay === text; }, function () {
                        var divider = element(doc, 'div', 'sn-divider');

                        if (day === view.today) divider.appendChild(element(doc, 'b', '', word('today')));

                        divider.appendChild(element(doc, 'span', '', dayText(label, day)));
                        divider.__snDay = text;

                        return divider;
                    }));
                }

                var key = 'r' + row.seq;
                var isOpen = ui.open === row.seq;
                var line = old[key];

                if (!line || line.__snRow !== row || line.__snOpen !== isOpen || line.__snText !== view.text) line = buildRow(phone, row, isOpen);
                else setClass(line, 'is-unread', !!row.unread);

                nodes[key] = line;
                wanted.push(line);
            }

            if (wanted.length === 0) wanted.push(keep(old, nodes, 'none', sameText, function () { return emptyBlock(doc, word('emptyFilter'), ''); }));

            if (isCut) {
                var more = keep(old, nodes, 'more', function () { return true; }, function () { return element(doc, 'div', 'sn-more-rows'); });

                wanted.push(more);

                // Watched again whenever more rows were drawn, so a list still too short to fill the panel keeps
                // growing until it does.
                if (refs.revealLimit !== limit || refs.revealNode !== more) {
                    refs.revealLimit = limit;
                    refs.revealNode = more;
                    reveal.unobserve(more);
                    pendingReveal = { reveal: reveal, node: more };
                }
            }
        }

        for (var gone in old) {
            if (nodes[gone] !== old[gone] && old[gone].parentElement === scroll) scroll.removeChild(old[gone]);
        }

        var at = scroll.firstChild;

        for (var w = 0; w < wanted.length; w++) {
            if (wanted[w] === at) at = at.nextSibling;
            else scroll.insertBefore(wanted[w], at);
        }

        while (at) {
            var next = at.nextSibling;

            scroll.removeChild(at);
            at = next;
        }

        refs.nodes = nodes;

        // Observed only once it is in the list, where the observer can see it.
        if (pendingReveal) {
            pendingReveal.reveal.observe(pendingReveal.node);
            pendingReveal = null;
        }
    }

    // How many of the shown rows to draw: the rows drawn so far, and always far enough to reach the open row.
    function rowLimit() {
        var limit = ui.limit;

        if (ui.open === null) return limit;

        for (var i = 0, at = 0; i < view.rows.length; i++) {
            if (!isShown(view.rows[i], ui.filter)) continue;

            at++;

            if (view.rows[i].seq === ui.open) return Math.max(limit, at);
        }

        return limit;
    }

    // The foot is drawn again only when what it says changed.
    function drawFoot(phone, refs) {
        var doc = phone.doc;
        var foot = refs.foot;
        var key = (ui.asking ? 'ask' : 'kept') + '|' + (view.kept || 0) + '|' + (view.cap || 0);

        show(foot, view.rows.length > 0);

        if (refs.footKey === key && refs.footText === view.text) return;

        refs.footKey = key;
        refs.footText = view.text;
        clear(foot);

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

    // The newest unread notices on the phone's home screen, in the space the game leaves empty above the dock. Drawn
    // again only when those notices or the words changed.
    function drawPeek(phone) {
        var doc = phone.doc;
        var peek = phone.peek;

        if (!peek) return;

        var refs = peek.__snRefs;
        var unread = [];

        for (var i = 0; i < view.rows.length && unread.length < PEEK; i++) {
            if (view.rows[i].unread) unread.push(view.rows[i]);
        }

        var grid = peek.parentElement;

        if (grid) setClass(grid, 'slnotify-peeking', unread.length > 0);

        show(peek, unread.length > 0);

        if (refs.text === view.text && sameRows(refs.shown, unread)) return;

        refs.shown = unread;
        refs.text = view.text;
        clear(peek);

        if (unread.length === 0) return;

        var head = element(doc, 'div', 'sn-peek-head');
        var seeAll = element(doc, 'span', 'sn-seeall', word('seeAll'));

        head.appendChild(element(doc, 'span', '', word('recent')));
        onClick(seeAll, function () { openList(phone); });
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
            onClick(card, function () { openList(phone, row.seq); });
            peek.appendChild(card);
        });
    }

    function sameRows(was, now) {
        if (!was || was.length !== now.length) return false;

        for (var i = 0; i < now.length; i++) {
            if (was[i] !== now[i]) return false;
        }

        return true;
    }

    function drawBadge(phone) {
        if (!phone.icon) return;

        var refs = phone.icon.__snRefs;

        setText(refs.label, view.label);
        setText(refs.badge, view.unread > 99 ? '99+' : String(view.unread || 0));
        show(refs.badge, view.unread > 0);
    }

    function draw(phone) {
        drawBadge(phone);
        drawPeek(phone);

        if (!phone.view) return;

        var refs = phone.view.__snRefs;
        var isHeadDue = refs.revision !== revision;
        var isRowsDue = (refs.rowsRevision !== revision || refs.rowsLimit !== ui.limit) && isListOpen(phone);

        if (!isHeadDue && !isRowsDue) return;

        var label = dayLabel(phone.doc);

        if (isHeadDue) {
            refs.revision = revision;
            drawHead(refs, label);
            drawFilters(refs);
            drawFoot(phone, refs);
        }

        if (isRowsDue) {
            refs.rowsRevision = revision;
            refs.rowsLimit = ui.limit;
            drawRows(phone, refs, label);
        }
    }

    function drawNow(phone) {
        try {
            draw(phone);
        } catch (e) {
            report('phone update failed: ' + e);
        }
    }

    // One pass puts back what is missing and draws what changed.
    function pass(phone) {
        phone.pending = false;

        if (phone.isDropped) return;

        try {
            ensure(phone);
            draw(phone);
        } catch (e) {
            report('phone update failed: ' + e);
        }
    }

    // However many changes come in a frame, a phone gets one pass, just before the frame is drawn.
    function schedule(phone) {
        if (phone.pending) return;

        phone.pending = true;

        if (typeof requestAnimationFrame !== 'function') {
            pass(phone);

            return;
        }

        requestAnimationFrame(function () { pass(phone); });
    }

    // A change on the phone's page needs a pass only when a part of the app is gone, or while a part is still missing
    // and the game adds something that might be where it goes. The app's own changes never need one. Nothing here
    // searches the page.
    function needsPass(phone, records) {
        if (phone.pending || phone.isDropped) return false;

        var isMissing = false;
        var parts = phone.parts;

        for (var p = 0; p < parts.length; p++) {
            var part = phone[parts[p]];

            if (!part) isMissing = true;
            else if (part.isConnected !== true) return true;
        }

        if (!isMissing) return false;

        if (!records || records.length === 0) return true;

        for (var i = 0; i < records.length; i++) {
            var added = records[i].addedNodes;

            for (var j = 0; added && j < added.length; j++) {
                if (added[j].nodeType === 1 && !added[j].__sn) return true;
            }
        }

        return false;
    }

    // The phone page redraws its dock and panel with Vue; an observer on the page puts the app back the frame a redraw
    // drops it, or the game first builds it, rather than at the next check.
    function watch(phone) {
        var Observer = phone.win.MutationObserver || window.MutationObserver;

        if (!Observer) return;

        phone.observer = new Observer(function (records) {
            try {
                if (needsPass(phone, records)) schedule(phone);
            } catch (e) {
                report('redraw failed: ' + e);
            }
        });
        phone.observer.observe(phone.doc, { childList: true, subtree: true });
        phone.watched = true;
    }

    // Still the page in its frame: a frame that was removed or loaded a new page drops the phone it held.
    function isCurrent(phone) {
        try {
            return phone.frame.isConnected !== false && phone.frame.contentWindow === phone.win && phone.win.document === phone.doc;
        } catch (e) {
            return false;
        }
    }

    function drop(at) {
        var phone = phones[at];

        phones.splice(at, 1);
        phone.isDropped = true;

        try {
            if (phone.observer && phone.observer.disconnect) phone.observer.disconnect();
            if (phone.doc.__slnotifyPhone === phone) phone.doc.__slnotifyPhone = null;
        } catch (e) {
            // The page is already gone.
        }
    }

    function prune() {
        for (var i = phones.length - 1; i >= 0; i--) {
            if (!isCurrent(phones[i])) drop(i);
        }
    }

    // The phone for a page found in a frame: the one already kept for it, or a new one, drawn at once and watched.
    function track(found) {
        var had = found.doc.__slnotifyPhone;

        if (had && !had.isDropped) return had;

        var phone = {
            frame: found.frame, win: found.win, doc: found.doc, pending: false, watched: false, isDropped: false,
            parts: ['style', 'icon', 'peek', 'view', 'appRoot'], style: null, icon: null, peek: null, view: null, appRoot: null, observer: null
        };

        found.doc.__slnotifyPhone = phone;
        found.frame.__slnotifyPhone = phone;
        phones.push(phone);
        pass(phone);
        watch(phone);

        return phone;
    }

    function adopt(frame) {
        var found = phoneIn(frame);

        if (!found) return false;

        track(found);

        return true;
    }

    // The game builds a new phone page every time the phone opens and shows it two frames after the page starts, long
    // before the next check. So a phone page is watched from the moment its frame appears, and the app goes in while
    // the game builds the page. A new frame first holds an empty page; the phone page replaces it a moment later, so
    // the frame is looked at each frame until it is there, for about two seconds at most. A frame is chased once at a
    // time, however often it is pointed at the phone.
    function chase(frame) {
        if (adopt(frame)) return;

        frame.__slnotifyChaseLeft = CHASE_FRAMES;

        if (frame.__slnotifyChasing || typeof requestAnimationFrame !== 'function') return;

        frame.__slnotifyChasing = true;

        var step = function () {
            frame.__slnotifyChaseLeft--;

            if (adopt(frame) || frame.__slnotifyChaseLeft <= 0) {
                frame.__slnotifyChasing = false;

                return;
            }

            requestAnimationFrame(step);
        };

        requestAnimationFrame(step);
    }

    function onFrame(frame) {
        if (!PHONE_PAGE.test(String(frame.src || ''))) return;

        if (!frame.__slnotifyFrame) {
            frame.__slnotifyFrame = true;
            frame.addEventListener('load', function () { chase(frame); });
        }

        chase(frame);
    }

    // Every game page is an iframe in this document: a new one, or one sent to a new address, is looked at at once.
    function watchFrames() {
        if (typeof MutationObserver === 'undefined') return;

        new MutationObserver(function (records) {
            for (var i = 0; i < records.length; i++) {
                var record = records[i];

                if (record.type === 'attributes') {
                    if (record.target.tagName === 'IFRAME') onFrame(record.target);

                    continue;
                }

                for (var j = 0; j < record.addedNodes.length; j++) {
                    var node = record.addedNodes[j];

                    if (node.nodeType !== 1) continue;

                    if (node.tagName === 'IFRAME') {
                        onFrame(node);
                        continue;
                    }

                    if (!node.firstChild) continue;

                    var inner = node.getElementsByTagName('iframe');

                    for (var k = 0; k < inner.length; k++) onFrame(inner[k]);
                }
            }
        }).observe(document, { childList: true, subtree: true, attributes: true, attributeFilter: ['src'] });
    }

    // The fallback for what the watchers miss: a phone page nobody found yet, or one whose page cannot be watched. A
    // phone that is watched is passed over without a look at its page.
    function check() {
        prune();

        var frames = document.querySelectorAll('iframe');

        for (var i = 0; i < frames.length; i++) {
            var kept = frames[i].__slnotifyPhone;

            if (kept && kept.watched && !kept.isDropped && isCurrent(kept)) continue;

            var found = phoneIn(frames[i]);

            if (!found) continue;

            var had = found.doc.__slnotifyPhone;
            var phone = track(found);

            if (had === phone && !phone.watched) pass(phone);
        }
    }

    // A list from the plugin. A row the page already has comes as its number; a number the page does not know means
    // the two disagree, so the page asks for the whole list and keeps what it shows until then.
    function set(next) {
        if (!next || !next.rows) return;

        var rows = [];
        var byNumber = {};

        for (var i = 0; i < next.rows.length; i++) {
            var row = next.rows[i];

            if (typeof row === 'number') {
                row = known[row];

                if (!row) {
                    known = {};
                    post('resync');

                    return;
                }
            }

            rows.push(row);
            byNumber[row.seq] = row;
        }

        known = byNumber;
        next.rows = rows;

        if (!next.text) next.text = view.text;

        view = next;

        if (ui.open !== null && !findRow(ui.open)) ui.open = null;
        if (view.rows.length === 0) ui.asking = false;

        changed();
    }

    window.__slnotify = { set: set, check: check };

    watchFrames();
    setInterval(check, CHECK_MS);
    check();
})();
