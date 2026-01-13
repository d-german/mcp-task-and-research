// Keyboard shortcuts handler for Task Manager UI
window.keyboardShortcuts = {
    _dotNetRef: null,
    _boundHandler: null,
    
    init: function(dotNetRef) {
        // Dispose any existing handler first
        this.dispose();
        
        this._dotNetRef = dotNetRef;
        // Store the bound handler so we can remove the same reference
        this._boundHandler = this.handleKeyDown.bind(this);
        document.addEventListener('keydown', this._boundHandler);
    },
    
    dispose: function() {
        if (this._boundHandler) {
            document.removeEventListener('keydown', this._boundHandler);
            this._boundHandler = null;
        }
        this._dotNetRef = null;
    },
    
    handleKeyDown: function(e) {
        if (!this._dotNetRef) return;
        
        // Skip if focused on input/textarea
        const target = e.target;
        if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable) {
            // Only allow Escape in inputs
            if (e.key === 'Escape') {
                target.blur();
                this._dotNetRef.invokeMethodAsync('OnEscape');
            }
            return;
        }
        
        // Ctrl/Cmd + N: New Task
        if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
            e.preventDefault();
            this._dotNetRef.invokeMethodAsync('OnNewTask');
        }
        // Ctrl/Cmd + F: Focus Search
        else if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
            e.preventDefault();
            this._dotNetRef.invokeMethodAsync('OnFocusSearch');
        }
        // Ctrl/Cmd + S: Save
        else if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            this._dotNetRef.invokeMethodAsync('OnSave');
        }
        // Escape: Close dialog/cancel
        else if (e.key === 'Escape') {
            this._dotNetRef.invokeMethodAsync('OnEscape');
        }
        // Ctrl/Cmd + R: Refresh
        else if ((e.ctrlKey || e.metaKey) && e.key === 'r') {
            e.preventDefault();
            this._dotNetRef.invokeMethodAsync('OnRefresh');
        }
        // ? : Show help
        else if (e.key === '?' && !e.ctrlKey && !e.metaKey) {
            this._dotNetRef.invokeMethodAsync('OnShowHelp');
        }
    },
    
    focusElement: function(selector) {
        const el = document.querySelector(selector);
        if (el) {
            el.focus();
        }
    }
};
