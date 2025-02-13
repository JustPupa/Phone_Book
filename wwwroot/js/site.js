// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function changeScale(elem) {
    a = document.querySelectorAll('.myDiv');
    a[0].style.transform = "scale(" + (1 + elem.value / 10) + ") translateY(" + elem.value * 23 + "px)";
    foot = document.querySelector('footer');
    foot.style.transform = "translateY(" + elem.value * 69 + "px)";
};

function HideWhatsNew() {
    SetWNBhidden(true);
}

function ShowWhatsNew() {
    SetWNBhidden(false);
}

function SetWNBhidden(value) {
    const wnblock = document.querySelector(".whatsNewBlock");
    wnblock.hidden = value;
}