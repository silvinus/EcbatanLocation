window.viewportInterop = {
    _query: null,
    _handler: null,

    // Returns true when the viewport matches the mobile breakpoint.
    isMobile: function () {
        return window.matchMedia('(max-width: 768px)').matches;
    },

    // Registers a listener that notifies .NET whenever the breakpoint is crossed.
    register: function (dotNetRef) {
        this._query = window.matchMedia('(max-width: 768px)');
        this._handler = function (e) {
            dotNetRef.invokeMethodAsync('OnViewportChanged', e.matches);
        };
        // addEventListener('change') is supported by all current browsers.
        this._query.addEventListener('change', this._handler);
        return this._query.matches;
    },

    unregister: function () {
        if (this._query && this._handler) {
            this._query.removeEventListener('change', this._handler);
        }
        this._query = null;
        this._handler = null;
    }
};
