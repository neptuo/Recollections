const _instances = new WeakMap();

function getOffsets(state) {
    const w = state.container.offsetWidth;
    const gap = parseFloat(getComputedStyle(state.track).columnGap) || 0;
    return {
        center: -(w + gap),
        prev: 0,
        next: -(2 * w + 2 * gap)
    };
}

export function initialize(dotnetRef, container) {
    if (_instances.has(container)) {
        _instances.get(container).dotnetRef = dotnetRef;
        return;
    }

    const track = container.querySelector('.swipe-track');
    if (!track) return;

    const state = {
        dotnetRef,
        container,
        track,
        startX: 0,
        startY: 0,
        currentDelta: 0,
        isSwiping: false,
        isHorizontalSwipe: null
    };

    state.onTouchStart = (e) => {
        if (e.touches.length !== 1) return;
        state.startX = e.touches[0].clientX;
        state.startY = e.touches[0].clientY;
        state.currentDelta = 0;
        state.isSwiping = true;
        state.isHorizontalSwipe = null;
    };

    state.onTouchMove = (e) => {
        if (!state.isSwiping || e.touches.length !== 1) return;

        const deltaX = e.touches[0].clientX - state.startX;
        const deltaY = e.touches[0].clientY - state.startY;

        if (state.isHorizontalSwipe === null) {
            if (Math.abs(deltaX) < 10 && Math.abs(deltaY) < 10) return;
            state.isHorizontalSwipe = Math.abs(deltaX) > Math.abs(deltaY);
            if (!state.isHorizontalSwipe) {
                state.isSwiping = false;
                return;
            }
            state.track.style.transition = 'none';
        }

        if (!state.isHorizontalSwipe) return;

        e.preventDefault();

        state.currentDelta = deltaX;
        const offsets = getOffsets(state);
        state.track.style.transform = `translateX(${offsets.center + deltaX}px)`;
    };

    state.onTouchEnd = () => {
        if (!state.isSwiping || state.isHorizontalSwipe !== true) {
            state.isSwiping = false;
            return;
        }

        state.isSwiping = false;
        const containerWidth = state.container.offsetWidth;
        const threshold = containerWidth * 0.2;
        const offsets = getOffsets(state);

        state.track.style.transition = '';

        if (Math.abs(state.currentDelta) > threshold) {
            const direction = state.currentDelta > 0 ? 'prev' : 'next';
            const targetPx = direction === 'prev' ? offsets.prev : offsets.next;
            state.track.style.transform = `translateX(${targetPx}px)`;

            state.track.addEventListener('transitionend', async () => {
                state.track.style.transition = 'none';
                state.track.style.transform = '';
                state.track.offsetHeight;
                state.track.style.transition = '';

                await state.dotnetRef.invokeMethodAsync('OnSwipeCompleted', direction);
            }, { once: true });
        } else {
            state.track.style.transform = '';
        }

        state.currentDelta = 0;
    };

    container.addEventListener('touchstart', state.onTouchStart, { passive: true });
    container.addEventListener('touchmove', state.onTouchMove, { passive: false });
    container.addEventListener('touchend', state.onTouchEnd, { passive: true });

    _instances.set(container, state);
}

export function resetPosition(container) {
    const state = _instances.get(container);
    if (!state) return;

    state.track.style.transition = 'none';
    state.track.style.transform = '';
    state.track.offsetHeight;
    state.track.style.transition = '';
}

export function dispose(container) {
    const state = _instances.get(container);
    if (!state) return;

    container.removeEventListener('touchstart', state.onTouchStart);
    container.removeEventListener('touchmove', state.onTouchMove);
    container.removeEventListener('touchend', state.onTouchEnd);

    _instances.delete(container);
}
