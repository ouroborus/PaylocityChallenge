window.app = window.app || {};
Object.assign(window.app, {
    loadingPanel: document.querySelector('.loading'),
    loading: (loading) => {
        const wasLoading = app.loadingPanel.classList.contains('hide');
        loading == undefined ? 0 : app.loadingPanel.classList[loading ? 'remove' : 'add']('hide');
        return wasLoading;
    },
});

$(function () {
    $('[data-toggle="tooltip"]').tooltip();
});
