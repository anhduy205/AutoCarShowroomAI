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

  const setOpen = (open) => {
    panel.hidden = !open;
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
    bubble.className = `chatbot-message ${role}`;
    bubble.textContent = text;
    messages.appendChild(bubble);
    messages.scrollTop = messages.scrollHeight;
  };

  const appendTyping = () => {
    const bubble = document.createElement("div");
    bubble.className = "chatbot-message bot chatbot-typing";
    bubble.textContent = "Dang tra loi...";
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

  toggleBtn?.addEventListener("click", () => setOpen(panel.hidden));
  closeBtn?.addEventListener("click", () => setOpen(false));

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
  });

  form?.addEventListener("submit", async (e) => {
    e.preventDefault();
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
        appendMessage(problem?.detail || "Chatbot dang ban. Vui long thu lai.", "bot");
        return;
      }

      const data = await res.json();
      typing.remove();
      appendMessage(data.reply || "(Khong co phan hoi)", "bot");
    } catch {
      typing.remove();
      appendMessage("Khong the ket noi chatbot. Vui long thu lai.", "bot");
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

