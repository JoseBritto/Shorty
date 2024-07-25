// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.onload = function() {
    window.addEventListener('click', function (e) {
        if (!document.getElementById('mobile-drawer').contains(e.target)) {
            document.getElementById('menu-btn').classList.remove('open');
            document.getElementById('mobile-nav').classList.remove('slide-in');
            document.getElementById('mobile-nav').classList.add('slide-out');
            document.getElementById('container-noboot-shrinker').classList.remove('shrink');
        }
    });
}