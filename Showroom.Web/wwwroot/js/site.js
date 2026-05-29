document.addEventListener("click", function (event) {
  const carThumbButton = event.target.closest("[data-car-thumb]");
  if (carThumbButton) {
    const gallery = carThumbButton.closest("[data-car-gallery]");
    const container = gallery?.closest(".car-detail-page") || gallery?.closest(".panel-card") || document;
    const heroImg = container.querySelector("[data-car-hero-img]");
    const lightboxImg = container.querySelector("[data-car-lightbox-img]");
    const indexLabel = container.querySelector("[data-car-photo-index]");
    const url = carThumbButton.getAttribute("data-car-thumb");
    const index = carThumbButton.getAttribute("data-car-thumb-index");
    if (heroImg && url) {
      heroImg.setAttribute("src", url);
    }
    if (lightboxImg && url) {
      lightboxImg.setAttribute("src", url);
    }
    if (indexLabel && index) {
      indexLabel.textContent = index;
    }
    gallery?.querySelectorAll("[data-car-thumb]").forEach((button) => {
      button.classList.toggle("active", button === carThumbButton);
    });
    return;
  }

  const lightboxOpen = event.target.closest("[data-car-lightbox-open]");
  if (lightboxOpen) {
    const page = lightboxOpen.closest(".car-detail-page");
    const lightbox = page?.querySelector("[data-car-lightbox]");
    if (lightbox) {
      lightbox.hidden = false;
      document.body.classList.add("lightbox-open");
    }
    return;
  }

  const lightboxClose = event.target.closest("[data-car-lightbox-close]");
  if (lightboxClose) {
    const lightbox = lightboxClose.closest("[data-car-lightbox]");
    if (lightbox) {
      lightbox.hidden = true;
      document.body.classList.remove("lightbox-open");
    }
    return;
  }

  const addButton = event.target.closest("[data-add-order-item]");
  if (addButton) {
    const form = addButton.closest("[data-order-form]");
    const list = form?.querySelector("[data-order-item-list]");
    const template = form?.querySelector("#order-item-template");

    if (!list || !template) {
      return;
    }

    const index = list.querySelectorAll("[data-order-item-row]").length;
    const markup = template.innerHTML
      .replaceAll("__index__", String(index))
      .replaceAll("__number__", String(index + 1));

    list.insertAdjacentHTML("beforeend", markup);
    return;
  }

  const removeButton = event.target.closest("[data-remove-order-item]");
  if (removeButton) {
    const list = removeButton.closest("[data-order-item-list]");
    const rows = list?.querySelectorAll("[data-order-item-row]");

    if (!list || !rows || rows.length <= 1) {
      return;
    }

    removeButton.closest("[data-order-item-row]")?.remove();
    reindexOrderRows(list);
  }
});

document.addEventListener("keydown", function (event) {
  if (event.key !== "Escape") {
    return;
  }

  const lightbox = document.querySelector("[data-car-lightbox]:not([hidden])");
  if (lightbox) {
    lightbox.hidden = true;
    document.body.classList.remove("lightbox-open");
  }
});

function reindexOrderRows(list) {
  const rows = list.querySelectorAll("[data-order-item-row]");
  rows.forEach(function (row, index) {
    const number = index + 1;
    const label = row.querySelector(".order-item-field .form-label");
    const carSelect = row.querySelector("select");
    const quantityInput = row.querySelector("input");
    const quantityLabel = row.querySelector(".order-item-qty .form-label");

    if (label) {
      label.setAttribute("for", `Items_${index}__CarId`);
      label.textContent = `Xe ${number}`;
    }

    if (carSelect) {
      carSelect.id = `Items_${index}__CarId`;
      carSelect.name = `Items[${index}].CarId`;
    }

    if (quantityLabel) {
      quantityLabel.setAttribute("for", `Items_${index}__Quantity`);
    }

    if (quantityInput) {
      quantityInput.id = `Items_${index}__Quantity`;
      quantityInput.name = `Items[${index}].Quantity`;
    }
  });
}
