// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".alarm-time").forEach(function (element) {

        const utcValue = element.dataset.utc;

        if (!utcValue) {
            return;
        }

        const date = new Date(utcValue);

        element.textContent = date.toLocaleString();
    });

});