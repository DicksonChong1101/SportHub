// ======================================================
// HOME HERO MODULE - LARGE IMAGE UPLOAD OPTIMIZER
// Resizes and compresses large Admin-selected photos in the browser before
// they are posted. This keeps the request below the server upload limit and
// prevents visitors from downloading an unnecessarily large hero image.
// ======================================================
(function () {
    "use strict";

    const input = document.getElementById("Image");
    const status = document.getElementById("hero-image-processing-status");

    if (!input || !status || !input.form) {
        return;
    }

    const form = input.form;
    const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
    const maximumWidth = 2400;
    const maximumHeight = 1600;
    const serverMaximumBytes = 8 * 1024 * 1024;

    function displaySize(bytes) {
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }

    function setSubmittingEnabled(enabled) {
        submitButtons.forEach(button => {
            button.disabled = !enabled;
        });
    }

    function loadImage(file) {
        return new Promise((resolve, reject) => {
            const image = new Image();
            const objectUrl = URL.createObjectURL(file);

            image.onload = function () {
                URL.revokeObjectURL(objectUrl);
                resolve(image);
            };

            image.onerror = function () {
                URL.revokeObjectURL(objectUrl);
                reject(new Error("The selected image could not be read."));
            };

            image.src = objectUrl;
        });
    }

    function createJpeg(canvas, quality) {
        return new Promise(resolve => {
            canvas.toBlob(resolve, "image/jpeg", quality);
        });
    }

    input.addEventListener("change", async function () {
        const originalFile = input.files && input.files[0];

        if (!originalFile) {
            status.textContent = "";
            return;
        }

        setSubmittingEnabled(false);
        status.className = "form-text text-primary";
        status.textContent = `Optimizing ${displaySize(originalFile.size)} image for the website...`;

        try {
            const image = await loadImage(originalFile);
            const scale = Math.min(
                1,
                maximumWidth / image.naturalWidth,
                maximumHeight / image.naturalHeight);
            const width = Math.max(1, Math.round(image.naturalWidth * scale));
            const height = Math.max(1, Math.round(image.naturalHeight * scale));
            const canvas = document.createElement("canvas");

            canvas.width = width;
            canvas.height = height;

            const context = canvas.getContext("2d");
            context.fillStyle = "#ffffff";
            context.fillRect(0, 0, width, height);
            context.drawImage(image, 0, 0, width, height);

            let optimizedBlob = null;

            for (const quality of [0.84, 0.72, 0.60]) {
                optimizedBlob = await createJpeg(canvas, quality);

                if (optimizedBlob && optimizedBlob.size <= serverMaximumBytes) {
                    break;
                }
            }

            if (!optimizedBlob || optimizedBlob.size > serverMaximumBytes) {
                throw new Error("The image is still too large after optimization. Please choose another photo.");
            }

            const baseName = originalFile.name.replace(/\.[^/.]+$/, "") || "hero-banner";
            const optimizedFile = new File(
                [optimizedBlob],
                `${baseName}-web.jpg`,
                { type: "image/jpeg", lastModified: Date.now() });
            const transfer = new DataTransfer();

            transfer.items.add(optimizedFile);
            input.files = transfer.files;

            status.className = "form-text text-success";
            status.textContent =
                `Ready: ${image.naturalWidth}×${image.naturalHeight} ${displaySize(originalFile.size)} ` +
                `→ ${width}×${height} ${displaySize(optimizedFile.size)}.`;
        } catch (error) {
            input.value = "";
            status.className = "form-text text-danger";
            status.textContent = error instanceof Error
                ? error.message
                : "The selected image could not be optimized.";
        } finally {
            setSubmittingEnabled(true);
        }
    });
})();
