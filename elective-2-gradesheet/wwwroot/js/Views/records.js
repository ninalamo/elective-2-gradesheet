// wwwroot/js/views/records.js
document.addEventListener('DOMContentLoaded', function () {
    // Handle collapse icon rotation
    var collapseToggles = document.querySelectorAll('.card-header button[data-bs-toggle="collapse"]');
    collapseToggles.forEach(function (toggle) {
        var collapseElement = document.querySelector(toggle.getAttribute('data-bs-target'));
        var icon = toggle.querySelector('.collapse-icon');

        if (collapseElement.classList.contains('show')) {
            icon.classList.add('fa-rotate-180');
        }

        collapseElement.addEventListener('show.bs.collapse', function () {
            icon.classList.add('fa-rotate-180');
        });

        collapseElement.addEventListener('hide.bs.collapse', function () {
            icon.classList.remove('fa-rotate-180');
        });
    });

    // Enable Bootstrap tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
