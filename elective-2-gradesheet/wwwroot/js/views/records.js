document.addEventListener('DOMContentLoaded', function () {
    // Add event listeners for collapse icons to rotate
    var collapseToggles = document.querySelectorAll('.card-header button[data-bs-toggle="collapse"]');
    collapseToggles.forEach(function(toggle) {
        var collapseElement = document.querySelector(toggle.getAttribute('data-bs-target'));
        var icon = toggle.querySelector('.collapse-icon');

        // Initial state check (if you want some to be open by default)
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

    // Initialize Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
