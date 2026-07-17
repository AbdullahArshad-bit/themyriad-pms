const Toast = {
    init() {
        this.hideTimeout = null;

        this.el = document.createElement("div");
        this.el.className = "toast";
        document.body.appendChild(this.el);
    },

    show(message, state, time=10000) {
        clearTimeout(this.hideTimeout);

        //this.el.textContent = message;
        $(this.el).html(message + "<i class='fa fa-remove toast-close' onclick='Toast.close()'></i>")
        this.el.className = "toast toast--visible";

        if (state) {
            this.el.classList.add(`toast--${state}`);
        }
        
        if (time > 0) {
            this.hideTimeout = setTimeout(() => {
                this.el.classList.remove("toast--visible");
            }, time);
        }

    },

    close() {
        this.el.classList.remove("toast--visible");
    }
};

document.addEventListener("DOMContentLoaded", () => Toast.init());