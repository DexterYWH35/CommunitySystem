// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Lightweight AJAX helpers for inline updates (likes/comments) without page reload.
(() => {
  function isHtmlResponse(response) {
    const contentType = response.headers.get("content-type") || "";
    return contentType.includes("text/html");
  }

  function isJsonResponse(response) {
    const contentType = response.headers.get("content-type") || "";
    return contentType.includes("application/json");
  }

  function setButtonState(button, hasLiked, likeLabel, unlikeLabel) {
    if (!button) return;
    button.classList.toggle("btn-primary", hasLiked);
    button.classList.toggle("btn-outline-primary", !hasLiked);

    const textNode = button.querySelector("[data-like-text]");
    if (textNode) {
      textNode.textContent = hasLiked ? unlikeLabel : likeLabel;
    }
  }

  async function handleAjaxSubmit(form) {
    const kind = form.getAttribute("data-ajax-kind") || "";
    const targetSelector = form.getAttribute("data-ajax-target") || "";

    const submitButton = form.querySelector("button[type='submit']");
    const previousDisabled = submitButton?.disabled ?? false;
    if (submitButton) submitButton.disabled = true;

    try {
      const response = await fetch(form.action, {
        method: (form.method || "POST").toUpperCase(),
        body: new FormData(form),
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: kind === "post-comments" ? "text/html" : "application/json",
        },
      });

      if (!response.ok) {
        return;
      }

      if (isJsonResponse(response)) {
        const payload = await response.json();

        if (kind === "post-like") {
          const countEl = document.querySelector("#post-like-count");
          if (countEl && typeof payload.likeCount === "number") {
            countEl.textContent = `${payload.likeCount} like(s)`;
          }

          setButtonState(
            submitButton,
            !!payload.hasLiked,
            "Like Post",
            "Unlike Post"
          );
        }

        if (kind === "comment-like") {
          const commentId = String(payload.commentId ?? "");
          const likeForm = document.querySelector(
            `form[data-ajax-kind='comment-like'][data-comment-id='${CSS.escape(commentId)}']`
          );
          const likeButton = likeForm?.querySelector("button[type='submit']");
          const likeCount = likeForm?.querySelector("[data-comment-like-count]");

          if (likeCount && typeof payload.likeCount === "number") {
            likeCount.textContent = String(payload.likeCount);
          }

          setButtonState(likeButton, !!payload.hasLiked, "Like", "Unlike");
        }

        return;
      }

      if (isHtmlResponse(response)) {
        const html = await response.text();
        if (!targetSelector) return;
        const target = document.querySelector(targetSelector);
        if (!target) return;
        target.innerHTML = html;
      }
    } finally {
      if (submitButton) submitButton.disabled = previousDisabled;
    }
  }

  document.addEventListener(
    "submit",
    (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) return;
      if (form.getAttribute("data-ajax") !== "true") return;

      event.preventDefault();
      handleAjaxSubmit(form);
    },
    true
  );
})();
