window.themeInterop = {
    apply: function (theme, mode) {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-mode', mode);
        localStorage.setItem('app-theme', theme);
        localStorage.setItem('app-mode', mode);
    },
    get: function () {
        return {
            theme: localStorage.getItem('app-theme') || 'ocean',
            mode: localStorage.getItem('app-mode') || 'dark'
        };
    }
};

window.newsInterop = {
    isCollapsed: function () {
        return localStorage.getItem('app-news-collapsed') === '1';
    },
    setCollapsed: function (collapsed) {
        localStorage.setItem('app-news-collapsed', collapsed ? '1' : '0');
    }
};
