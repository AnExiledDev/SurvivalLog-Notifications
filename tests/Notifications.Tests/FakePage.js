// Just enough of the game's root web page for notifications.js to run in Jint: the root document with its iframes,
// and the phone (the PhoneSMS page) in a frame of its own, drawn the way its template draws the home screen. Elements
// have classes, attributes, a parent, click listeners that bubble, and the few selectors the script uses. Test code
// only; nothing here ships.
var window = this;
var posted = [];
var timers = [];

window.location = { href: 'https://ui.local/index.html' };
window.vuplex = { postMessage: function (message) { posted.push(String(message)); } };

function setInterval(callback) {
    timers.push(callback);

    return timers.length;
}

// What the page's own interval would do on its next tick.
function runTimers() {
    timers.slice().forEach(function (callback) { callback(); });
}

var FakeElement = class {
    constructor(tagName, className) {
        var element = this;

        this.tagName = String(tagName).toUpperCase();
        this.id = '';
        this.parentElement = null;
        this.children = [];
        this.textContent = '';
        this.attrs = {};
        this.listeners = {};
        this.classes = className ? className.split(' ') : [];
        this.classList = {
            contains: function (name) { return element.classes.indexOf(name) >= 0; },
            add: function (name) { if (element.classes.indexOf(name) < 0) element.classes.push(name); },
            remove: function (name) { element.classes = element.classes.filter(function (c) { return c !== name; }); }
        };
    }

    get className() { return this.classes.join(' '); }

    set className(value) { this.classes = String(value).split(' ').filter(function (c) { return c; }); }

    get firstChild() { return this.children[0] || null; }

    get nextSibling() {
        if (!this.parentElement) return null;

        var siblings = this.parentElement.children;

        return siblings[siblings.indexOf(this) + 1] || null;
    }

    // On a page: the element's top ancestor is a document's root element.
    get isConnected() {
        var top = this;

        while (top.parentElement) top = top.parentElement;

        return !!top.isDocumentRoot;
    }

    setAttribute(name, value) { this.attrs[name] = String(value); }

    getAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attrs, name) ? this.attrs[name] : null; }

    appendChild(child) {
        if (child.parentElement) child.parentElement.removeChild(child);

        child.parentElement = this;
        this.children.push(child);

        return child;
    }

    removeChild(child) {
        var at = this.children.indexOf(child);

        if (at < 0) throw new Error('removeChild: not a child of this element');

        this.children.splice(at, 1);
        child.parentElement = null;

        return child;
    }

    // Moves the child before ref, or to the end when ref is null, out of wherever it was.
    insertBefore(child, ref) {
        if (child.parentElement) {
            var from = child.parentElement.children;

            from.splice(from.indexOf(child), 1);
        }

        var at = ref ? this.children.indexOf(ref) : -1;

        if (ref && at < 0) throw new Error('insertBefore: the reference is not a child of this element');

        child.parentElement = this;

        if (at < 0) this.children.push(child);
        else this.children.splice(at, 0, child);

        return child;
    }

    remove() {
        if (this.parentElement) this.parentElement.removeChild(this);
    }

    addEventListener(type, listener) {
        (this.listeners[type] = this.listeners[type] || []).push(listener);
    }

    // A click runs this element's listeners, then each parent's, until one stops it. An element drawn with Vue's
    // @click.stop stops it after its own listeners, as the phone's panel does.
    click() {
        var isStopped = false;
        var event = { type: 'click', target: this, stopPropagation: function () { isStopped = true; } };

        for (var element = this; element && !isStopped; element = element.parentElement) {
            (element.listeners.click || []).slice().forEach(function (listener) { listener(event); });

            if (element.stopsClicks) break;
        }
    }

    // "a b" is the descendant combinator: the last step names this element, each step before it some element above.
    matches(selector) {
        var steps = selector.trim().split(/\s+/);

        if (!matchesStep(this, steps[steps.length - 1])) return false;

        var element = this.parentElement;

        for (var i = steps.length - 2; i >= 0; i--) {
            while (element && !matchesStep(element, steps[i])) element = element.parentElement;

            if (!element) return false;

            element = element.parentElement;
        }

        return true;
    }

    querySelectorAll(selector) {
        return descendantsOf(this).filter(function (element) { return element.matches(selector); });
    }

    querySelector(selector) {
        return this.querySelectorAll(selector)[0] || null;
    }
};

// A step is a tag name, classes, or both: "iframe", ".app-dock", "div.app.is-sms".
function matchesStep(element, step) {
    var parts = /^([a-zA-Z]*)((?:\.[\w-]+)*)$/.exec(step);

    if (!parts) throw new Error('the fake page does not know the selector "' + step + '"');

    if (parts[1] && element.tagName !== parts[1].toUpperCase()) return false;

    return parts[2].split('.').filter(function (c) { return c; }).every(function (c) { return element.classList.contains(c); });
}

function descendantsOf(root) {
    var found = [];

    root.children.forEach(function walk(child) {
        found.push(child);
        child.children.forEach(walk);
    });

    return found;
}

function fakeDocument() {
    var doc = {
        documentElement: new FakeElement('html'),
        getElementById: function (id) {
            return descendantsOf(doc.documentElement).filter(function (element) { return element.id === id; })[0] || null;
        },
        createElement: function (tagName) { return new FakeElement(tagName); },
        createElementNS: function (namespace, tagName) { return new FakeElement(tagName); },
        querySelector: function (selector) { return doc.documentElement.querySelector(selector); },
        querySelectorAll: function (selector) { return doc.documentElement.querySelectorAll(selector); }
    };

    doc.documentElement.isDocumentRoot = true;
    doc.head = doc.documentElement.appendChild(new FakeElement('head'));
    doc.body = doc.documentElement.appendChild(new FakeElement('body'));

    return doc;
}

var document = fakeDocument();

// A frame's window. Its observers are run by mutate(), standing in for the browser noticing the page changed.
function fakeWindow(location, doc) {
    var win = { location: location, document: doc, observers: [] };

    win.MutationObserver = function (callback) {
        var observer = { callback: callback, target: null };

        win.observers.push(observer);
        this.observe = function (target) { observer.target = target; };
    };
    win.mutate = function () {
        win.observers.filter(function (o) { return o.target; }).forEach(function (o) { o.callback([]); });
    };

    return win;
}

// The phone's home screen from PhoneSMS.html: #app holding the Vue state with the day label, the root that closes the
// phone when clicked outside the panel, the panel that stops clicks, the home view with its empty app grid, and the
// dock with its apps.
// spec: { dayLabel }
function buildPhoneDocument(spec) {
    var doc = fakeDocument();
    var app = doc.body.appendChild(new FakeElement('div'));
    var root = app.appendChild(new FakeElement('div', 'app-root'));
    var panel = root.appendChild(new FakeElement('div', 'sms-panel'));
    var home = panel.appendChild(new FakeElement('div', 'view view-home is-active'));
    home.appendChild(new FakeElement('div', 'app-grid'));
    var dock = home.appendChild(new FakeElement('div', 'app-dock'));

    app.id = 'app';
    app._vnode = { component: { setupState: { state: { localText: { dayLabel: spec.dayLabel } } } } };
    panel.stopsClicks = true;
    doc.closedCount = 0;
    root.addEventListener('click', function () { doc.closedCount++; });
    dock.appendChild(new FakeElement('div', 'app is-sms'));
    dock.appendChild(new FakeElement('div', 'app is-loanshark'));

    return doc;
}

// Puts a phone frame in the root page and returns its window.
function addPhone(spec) {
    var frame = document.body.appendChild(new FakeElement('iframe'));
    var win = fakeWindow('https://ui.local/UI/PhoneSMS/PhoneSMS.html', buildPhoneDocument(spec || {}));

    frame.contentWindow = win;

    return win;
}

// A frame from another origin: reading its location throws.
function addForeignFrame() {
    var frame = document.body.appendChild(new FakeElement('iframe'));
    var win = { document: null };

    Object.defineProperty(win, 'location', { get: function () { throw new Error('cross-origin'); } });
    frame.contentWindow = win;

    return win;
}

// The phone frame loading its page again: a fresh document with none of the app in it.
function reloadPhone(win, spec) {
    win.document = buildPhoneDocument(spec || {});
    win.observers = [];
}

// Every text under an element, in order.
function textOf(element) {
    return [element.textContent].concat(element.children.map(textOf)).filter(function (t) { return t; }).join(' ');
}
