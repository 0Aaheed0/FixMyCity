// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Lightweight progressive enhancement for the shared premium UI.
document.addEventListener('DOMContentLoaded', () => {
    const cards = document.querySelectorAll('.fmc-app-card, .card, .fmc-feature-card, .fmc-feed-item, .fmc-action-tile, .fmc-auth-card');
    cards.forEach((element, index) => {
        element.style.animationDelay = `${Math.min(index * 45, 360)}ms`;
        element.classList.add('fmc-animated-card');
        element.classList.add('fmc-reveal', index % 2 ? 'fmc-reveal-right' : 'fmc-reveal-left');
    });

    const reveal = entries => entries.forEach(entry => {
        // Keep observing so cards animate both entering and leaving the viewport.
        entry.target.classList.toggle('is-visible', entry.isIntersecting);
    });
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver(reveal, { threshold: 0.28, rootMargin: '0px 0px -24px' });
        document.querySelectorAll('.fmc-reveal').forEach(element => observer.observe(element));
    } else {
        document.querySelectorAll('.fmc-reveal').forEach(element => element.classList.add('is-visible'));
    }
});
