// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {

    $("form").on("submit", function () {
        $(this).find("button[type=submit]").attr("disabled", "disabled")
    });
});