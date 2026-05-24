(() => {
  const root = document.querySelector("[data-chatbot]");
  if (!root) return;

  const toggleBtn = root.querySelector("[data-chatbot-toggle]");
  const panel = root.querySelector("[data-chatbot-panel]");
  const closeBtn = root.querySelector("[data-chatbot-close]");
  const form = root.querySelector("[data-chatbot-form]");
  const textInput = root.querySelector("[data-chatbot-text]");
  const messages = root.querySelector("[data-chatbot-messages]");
  const sendBtn = root.querySelector("[data-chatbot-send]");

  const presetsToggleBtn = root.querySelector("[data-chatbot-presets-toggle]");
  const presetsExtra = root.querySelector("[data-chatbot-presets-extra]");

  const setPresetsExpanded = (expanded) => {
    if (!presetsExtra || !presetsToggleBtn) return;
    presetsExtra.hidden = !expanded;
    presetsToggleBtn.setAttribute("aria-expanded", expanded ? "true" : "false");
    presetsToggleBtn.textContent = expanded ? "Thu gọn" : "Xem thêm";
  };

  const setOpen = (open) => {
    panel.hidden = !open;
    if (!open) {
      setPresetsExpanded(false);
    }

    try {
      localStorage.setItem("showroom.chatbot.open", open ? "1" : "0");
    } catch {
      // ignore
    }

    if (open) {
      textInput?.focus();
    }
  };

  const appendMessage = (text, role) => {
    const bubble = document.createElement("div");
    bubble.classNăme = `chatbot-message ${role}`;
    if (role === "bot") {
      bubble.innerHTML = renderBotText(text);
    } else {
      bubble.textContent = text;
    }
    messages.appendChild(bubble);
    messages.scrollTop = messages.scrollHeight;
  };

  const escapeHtml = (value) =>
    String(value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");

  const isSafeHref = (href) => {
    if (!href) return false;
    if (href.startsWith("/")) return true;
    return /^https?:\/\//i.test(href);
  };

  const renderBotText = (text) => {
    // Render a tiny safe subset of Markdown (bold + links + newlines + bullets).
    let html = escapeHtml(text || "");

    // [label](url)
    html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_m, label, href) => {
      const safeHref = String(href || "").trim();
      if (!isSafeHref(safeHref)) return label;
      return `<a href="${escapeHtml(safeHref)}" class="chatbot-link">${escapeHtml(label)}</a>`;
    });

    // auto-link /cars/123
    html = html.replace(/(^|[^"'=])(\/cars\/\d+)\b/g, (_m, prefix, path) => {
      return `${prefix}<a href="${escapeHtml(path)}" class="chatbot-link">${escapeHtml(path)}</a>`;
    });

    // **bold**
    html = html.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");

    // bullets: "\n- " => "\nâ€¢ "
    html = html.replace(/\n-\s+/g, "\n\u2022 ");

    // newlines
    html = html.replace(/\n/g, "<br/>");
    return html;
  };

  const appendTyping = () => {
    const bubble = document.createElement("div");
    bubble.classNăme = "chatbot-message bot chatbot-typing";
    bubble.textContent = "Đang trả lời...";
    messages.appendChild(bubble);
    messages.scrollTop = messages.scrollHeight;
    return bubble;
  };

  const openWithPrefill = (prefill) => {
    setOpen(true);
    if (prefill && typeof prefill === "string") {
      textInput.value = prefill;
      textInput.focus();
      textInput.setSelectionRange(textInput.value.length, textInput.value.length);
    }
  };

  // IME composition guard — fix double-type voi Unikey/Telex/VNI
  let isComposing = false;
  textInput.addEventListener("compositionstart", () => { isComposing = true; });
  textInput.addEventListener("compositionend",   () => { isComposing = false; });

  toggleBtn?.addEventListener("click", () => setOpen(panel.hidden));
  closeBtn?.addEventListener("click", () => setOpen(false));

  presetsToggleBtn?.addEventListener("click", () => {
    const expanded = presetsToggleBtn.getAttribute("aria-expanded") === "true";
    setPresetsExpanded(!expanded);
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && !panel.hidden) {
      setOpen(false);
    }
  });

  document.addEventListener("click", (e) => {
    const openButton = e.target.closest("[data-chatbot-open]");
    if (!openButton) return;
    const prefill = openButton.getAttribute("data-chatbot-prefill") || "";
    openWithPrefill(prefill);
    setPresetsExpanded(false);
  });

  form?.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (isComposing) return;  // Cho IME hoan thanh truoc khi gui
    const message = (textInput.value || "").trim();
    if (!message) return;

    textInput.value = "";
    appendMessage(message, "user");
    const typing = appendTyping();

    sendBtn.disabled = true;
    textInput.disabled = true;

    try {
      const res = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message }),
      });

      if (!res.ok) {
        const problem = await res.json().catch(() => null);
        typing.remove();
        appendMessage(problem?.detail || "Chatbot đang bận. Vui lòng thử lại.", "bot");
        return;
      }

      const data = await res.json();
      typing.remove();
      appendMessage(data.reply || "(Không có phản hồi)", "bot");
    } catch {
      typing.remove();
      appendMessage("Không thể kết nối chatbot. Vui lòng thử lại.", "bot");
    } finally {
      sendBtn.disabled = false;
      textInput.disabled = false;
      textInput.focus();
    }
  });

  try {
    const open = localStorage.getItem("showroom.chatbot.open") === "1";
    if (open) {
      setOpen(true);
    }
  } catch {
    // ignore
  }
})();
