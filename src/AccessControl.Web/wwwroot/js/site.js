// Общие улучшения интерфейса: фильтрация чекбокс-списков и поисковых списков.
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-selection-search-input]').forEach((input) => {
        const targetKey = input.getAttribute('data-selection-search-input');
        const list = document.querySelector(`[data-selection-search-target="${targetKey}"]`);
        if (!list) {
            return;
        }

        const items = Array.from(list.querySelectorAll('[data-filter-text]'));
        const filterItems = () => {
            const query = input.value.trim().toLowerCase();
            items.forEach((item) => {
                const haystack = item.getAttribute('data-filter-text') ?? '';
                item.classList.toggle('d-none', query.length > 0 && !haystack.includes(query));
            });
        };

        input.addEventListener('input', filterItems);
    });

    document.querySelectorAll('[data-select-filter-input]').forEach((input) => {
        const targetKey = input.getAttribute('data-select-filter-input');
        const select = document.querySelector(`[data-select-filter-target="${targetKey}"]`);
        if (!(select instanceof HTMLSelectElement)) {
            return;
        }

        const originalOptions = Array.from(select.options).map((option) => ({
            value: option.value,
            text: option.text,
            selected: option.selected,
            filterText: (option.dataset.filterText ?? option.text).toLowerCase()
        }));

        const renderOptions = () => {
            const query = input.value.trim().toLowerCase();
            const currentValue = select.value;
            const filtered = originalOptions.filter((option, index) => index === 0 || query.length === 0 || option.filterText.includes(query));

            select.innerHTML = '';
            filtered.forEach((option) => {
                const element = document.createElement('option');
                element.value = option.value;
                element.text = option.text;
                element.dataset.filterText = option.filterText;
                element.selected = option.value === currentValue || (!currentValue && option.selected);
                select.appendChild(element);
            });

            if (!Array.from(select.options).some((option) => option.value === currentValue)) {
                select.value = '';
            }
        };

        input.addEventListener('input', renderOptions);
    });
});
