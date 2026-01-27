// htmx configuration
document.body.addEventListener('htmx:configRequest', function(event) {
    // Add anti-forgery token to all requests
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token;
    }
});

// Handle toast notifications from HX-Trigger header
document.body.addEventListener('showToast', function(event) {
    const { message, type } = event.detail;
    showToast(message, type);
});

// Handle modal close from HX-Trigger header
document.body.addEventListener('closeModal', function() {
    const backdrops = document.querySelectorAll('.modal-backdrop');
    const modals = document.querySelectorAll('.modal');
    backdrops.forEach(el => el.remove());
    modals.forEach(el => el.remove());
    // Also clear the container if it was innerHTML-swapped
    const container = document.getElementById('modal-container');
    if (container) container.innerHTML = '';
});

// Handle artist count updates
document.body.addEventListener('updateCount', function(event) {
    const { count, start, end } = event.detail;
    const countElement = document.getElementById('artist-count') || 
                         document.getElementById('album-count') || 
                         document.getElementById('track-count');
    if (countElement && count !== undefined) {
        if (start !== undefined && end !== undefined) {
            countElement.textContent = `Showing ${start} to ${end} of ${count} items`;
        } else {
            countElement.textContent = `${count} items`;
        }
    }
});

// Update selected count for bulk actions
document.body.addEventListener('change', function(event) {
    if (event.target.matches('input[name="selectedTracks"]')) {
        updateSelectedCount();
    }
});

function updateSelectedCount() {
    const checked = document.querySelectorAll('input[name="selectedTracks"]:checked');
    const countElement = document.getElementById('selected-count');
    if (countElement) {
        countElement.textContent = checked.length;
    }
}

// Toast notification function
function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast ${getToastColorClass(type)} text-white px-4 py-3 rounded-lg shadow-lg flex items-center gap-3 min-w-[300px]`;
    toast.innerHTML = `
        <svg class="w-5 h-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            ${getToastIcon(type)}
        </svg>
        <span class="flex-1">${escapeHtml(message)}</span>
        <button type="button" class="text-white hover:text-gray-200" onclick="this.parentElement.remove()">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
            </svg>
        </button>
    `;

    container.appendChild(toast);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s ease-out';
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

function getToastColorClass(type) {
    switch (type) {
        case 'success': return 'bg-green-500';
        case 'error': return 'bg-red-500';
        case 'warning': return 'bg-yellow-500';
        default: return 'bg-blue-500';
    }
}

function getToastIcon(type) {
    switch (type) {
        case 'success':
            return "<path fill-rule='evenodd' d='M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z' clip-rule='evenodd'/>";
        case 'error':
            return "<path fill-rule='evenodd' d='M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z' clip-rule='evenodd'/>";
        case 'warning':
            return "<path fill-rule='evenodd' d='M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z' clip-rule='evenodd'/>";
        default:
            return "<path fill-rule='evenodd' d='M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z' clip-rule='evenodd'/>";
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Handle htmx errors
document.body.addEventListener('htmx:responseError', function(event) {
    console.error('htmx error:', event.detail);
    showToast('An error occurred. Please try again.', 'error');
});

// Handle htmx send error (network issues)
document.body.addEventListener('htmx:sendError', function(event) {
    console.error('htmx send error:', event.detail);
    showToast('Network error. Please check your connection.', 'error');
});

// Keyboard shortcuts
document.addEventListener('keydown', function(event) {
    // Escape key closes modals
    if (event.key === 'Escape') {
        const modal = document.querySelector('.modal');
        if (modal) {
            document.body.dispatchEvent(new CustomEvent('closeModal'));
        }
    }
});

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Any initialization code here
    console.log('Chinook Dashboard loaded');
});
