/**
 * Search Suggestions Module
 * Provides autocomplete functionality for search inputs
 */
(function () {
    'use strict';

    // Configuration
    const CONFIG = {
        minCharacters: 2,
        debounceDelay: 300 // milliseconds
    };

    // State
    let debounceTimer = null;
    let currentRequest = null;
    let activeSearchInput = null;

    /**
     * Initializes search suggestions for a given input element
     * @param {HTMLInputElement} searchInput - The search input element
     */
    function initializeSearchSuggestions(searchInput) {
        if (!searchInput) return;

        // Create and append dropdown container
        const dropdown = createDropdown();
        const container = searchInput.parentElement;
        
        // Ensure the container has position relative for absolute positioning
        if (getComputedStyle(container).position === 'static') {
            container.style.position = 'relative';
        }
        
        container.appendChild(dropdown);

        // Attach event listeners
        searchInput.addEventListener('input', handleInput);
        searchInput.addEventListener('focus', handleFocus);
        searchInput.addEventListener('keydown', handleKeyDown);
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!container.contains(e.target)) {
                hideDropdown(dropdown);
            }
        });
    }

    /**
     * Creates the dropdown container element
     * @returns {HTMLElement} The dropdown element
     */
    function createDropdown() {
        const dropdown = document.createElement('div');
        dropdown.className = 'search-suggestions-dropdown';
        dropdown.style.display = 'none';
        return dropdown;
    }

    /**
     * Handles input events with debouncing
     * @param {Event} e - The input event
     */
    function handleInput(e) {
        const input = e.target;
        activeSearchInput = input;
        const query = input.value.trim();
        const dropdown = input.parentElement.querySelector('.search-suggestions-dropdown');

        // Clear existing timer
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }

        // Hide dropdown if query is too short
        if (query.length < CONFIG.minCharacters) {
            hideDropdown(dropdown);
            return;
        }

        // Debounce the API call
        debounceTimer = setTimeout(() => {
            fetchSuggestions(query, input, dropdown);
        }, CONFIG.debounceDelay);
    }

    /**
     * Handles focus events
     * @param {Event} e - The focus event
     */
    function handleFocus(e) {
        const input = e.target;
        activeSearchInput = input;
        const query = input.value.trim();
        const dropdown = input.parentElement.querySelector('.search-suggestions-dropdown');

        // Show existing suggestions if available
        if (query.length >= CONFIG.minCharacters && dropdown.children.length > 0) {
            showDropdown(dropdown);
        }
    }

    /**
     * Handles keyboard navigation
     * @param {KeyboardEvent} e - The keyboard event
     */
    function handleKeyDown(e) {
        const dropdown = e.target.parentElement.querySelector('.search-suggestions-dropdown');
        if (!dropdown || dropdown.style.display === 'none') return;

        const items = dropdown.querySelectorAll('.suggestion-item');
        if (items.length === 0) return;

        const currentActive = dropdown.querySelector('.suggestion-item.active');
        let currentIndex = currentActive ? Array.from(items).indexOf(currentActive) : -1;

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                currentIndex = currentIndex < items.length - 1 ? currentIndex + 1 : currentIndex;
                setActiveItem(items, currentIndex);
                break;
            case 'ArrowUp':
                e.preventDefault();
                currentIndex = currentIndex > 0 ? currentIndex - 1 : 0;
                setActiveItem(items, currentIndex);
                break;
            case 'Enter':
                if (currentActive) {
                    e.preventDefault();
                    currentActive.click();
                }
                break;
            case 'Escape':
                e.preventDefault();
                hideDropdown(dropdown);
                break;
        }
    }

    /**
     * Sets the active item in the dropdown
     * @param {NodeList} items - The suggestion items
     * @param {number} index - The index of the item to activate
     */
    function setActiveItem(items, index) {
        items.forEach((item, i) => {
            if (i === index) {
                item.classList.add('active');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('active');
            }
        });
    }

    /**
     * Fetches suggestions from the API
     * @param {string} query - The search query
     * @param {HTMLInputElement} input - The search input element
     * @param {HTMLElement} dropdown - The dropdown element
     */
    async function fetchSuggestions(query, input, dropdown) {
        // Cancel previous request if still pending
        if (currentRequest) {
            currentRequest.abort();
        }

        // Create abort controller for this request
        const controller = new AbortController();
        currentRequest = controller;

        try {
            const url = `/Api/SearchSuggestions?q=${encodeURIComponent(query)}`;
            const response = await fetch(url, {
                signal: controller.signal,
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const suggestions = await response.json();
            renderSuggestions(suggestions, input, dropdown);
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error('Error fetching suggestions:', error);
                hideDropdown(dropdown);
            }
        } finally {
            if (currentRequest === controller) {
                currentRequest = null;
            }
        }
    }

    /**
     * Renders suggestions in the dropdown
     * @param {Array} suggestions - The suggestions array
     * @param {HTMLInputElement} input - The search input element
     * @param {HTMLElement} dropdown - The dropdown element
     */
    function renderSuggestions(suggestions, input, dropdown) {
        // Clear existing suggestions
        dropdown.innerHTML = '';

        if (!suggestions || suggestions.length === 0) {
            hideDropdown(dropdown);
            return;
        }

        // Group suggestions by type
        const grouped = groupSuggestionsByType(suggestions);

        // Render categories first
        if (grouped.category && grouped.category.length > 0) {
            const categoryHeader = createSectionHeader('Categories');
            dropdown.appendChild(categoryHeader);
            grouped.category.forEach(suggestion => {
                dropdown.appendChild(createSuggestionItem(suggestion, input));
            });
        }

        // Render products
        if (grouped.product && grouped.product.length > 0) {
            const productHeader = createSectionHeader('Products');
            dropdown.appendChild(productHeader);
            grouped.product.forEach(suggestion => {
                dropdown.appendChild(createSuggestionItem(suggestion, input));
            });
        }

        showDropdown(dropdown);
    }

    /**
     * Groups suggestions by type
     * @param {Array} suggestions - The suggestions array
     * @returns {Object} Grouped suggestions
     */
    function groupSuggestionsByType(suggestions) {
        return suggestions.reduce((acc, suggestion) => {
            const type = suggestion.type || 'other';
            if (!acc[type]) {
                acc[type] = [];
            }
            acc[type].push(suggestion);
            return acc;
        }, {});
    }

    /**
     * Creates a section header element
     * @param {string} title - The header title
     * @returns {HTMLElement} The header element
     */
    function createSectionHeader(title) {
        const header = document.createElement('div');
        header.className = 'suggestion-header';
        header.textContent = title;
        return header;
    }

    /**
     * Creates a suggestion item element
     * @param {Object} suggestion - The suggestion object
     * @param {HTMLInputElement} input - The search input element
     * @returns {HTMLElement} The suggestion item element
     */
    function createSuggestionItem(suggestion, input) {
        const item = document.createElement('a');
        item.className = 'suggestion-item';
        item.href = suggestion.url || '#';
        
        // Add icon based on type
        const icon = createIcon(suggestion.type);
        item.appendChild(icon);
        
        // Add text
        const text = document.createElement('span');
        text.textContent = suggestion.text;
        item.appendChild(text);

        // Handle click
        item.addEventListener('click', function(e) {
            if (suggestion.value) {
                e.preventDefault();
                input.value = suggestion.value;
                input.form.submit();
            }
            // Otherwise, allow default navigation
        });

        return item;
    }

    /**
     * Creates an icon element for a suggestion type
     * @param {string} type - The suggestion type
     * @returns {HTMLElement} The icon element
     */
    function createIcon(type) {
        const icon = document.createElement('span');
        icon.className = 'suggestion-icon';
        
        switch (type) {
            case 'category':
                icon.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16"><path d="M1 2.5A1.5 1.5 0 0 1 2.5 1h3A1.5 1.5 0 0 1 7 2.5v3A1.5 1.5 0 0 1 5.5 7h-3A1.5 1.5 0 0 1 1 5.5v-3zM2.5 2a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3zm6.5.5A1.5 1.5 0 0 1 10.5 1h3A1.5 1.5 0 0 1 15 2.5v3A1.5 1.5 0 0 1 13.5 7h-3A1.5 1.5 0 0 1 9 5.5v-3zm1.5-.5a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3zM1 10.5A1.5 1.5 0 0 1 2.5 9h3A1.5 1.5 0 0 1 7 10.5v3A1.5 1.5 0 0 1 5.5 15h-3A1.5 1.5 0 0 1 1 13.5v-3zm1.5-.5a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3zm6.5.5A1.5 1.5 0 0 1 10.5 9h3a1.5 1.5 0 0 1 1.5 1.5v3a1.5 1.5 0 0 1-1.5 1.5h-3A1.5 1.5 0 0 1 9 13.5v-3zm1.5-.5a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3z"/></svg>';
                break;
            case 'product':
                icon.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16"><path d="M0 1.5A.5.5 0 0 1 .5 1H2a.5.5 0 0 1 .485.379L2.89 3H14.5a.5.5 0 0 1 .491.592l-1.5 8A.5.5 0 0 1 13 12H4a.5.5 0 0 1-.491-.408L2.01 3.607 1.61 2H.5a.5.5 0 0 1-.5-.5zM3.102 4l1.313 7h8.17l1.313-7H3.102zM5 12a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm7 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm-7 1a1 1 0 1 1 0 2 1 1 0 0 1 0-2zm7 0a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/></svg>';
                break;
            default:
                icon.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16"><path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"/></svg>';
        }
        
        return icon;
    }

    /**
     * Shows the dropdown
     * @param {HTMLElement} dropdown - The dropdown element
     */
    function showDropdown(dropdown) {
        dropdown.style.display = 'block';
        // Add class to container for styling
        const container = dropdown.parentElement;
        if (container) {
            container.classList.add('suggestions-active');
        }
    }

    /**
     * Hides the dropdown
     * @param {HTMLElement} dropdown - The dropdown element
     */
    function hideDropdown(dropdown) {
        dropdown.style.display = 'none';
        // Remove class from container
        const container = dropdown.parentElement;
        if (container) {
            container.classList.remove('suggestions-active');
        }
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAll);
    } else {
        initializeAll();
    }

    /**
     * Initializes all search inputs on the page
     */
    function initializeAll() {
        // Find all search inputs with the data attribute
        const searchInputs = document.querySelectorAll('[data-search-suggestions]');
        searchInputs.forEach(input => {
            initializeSearchSuggestions(input);
        });
    }

    // Expose initialization function globally if needed
    window.SearchSuggestions = {
        init: initializeSearchSuggestions
    };
})();
