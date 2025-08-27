document.addEventListener('DOMContentLoaded', function () {
    // Collapse icon rotation logic...
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

    // ✅ Tooltip activation code
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
<script src="~/js/views/records.js"></script>
