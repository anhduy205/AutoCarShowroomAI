document.addEventListener("click", function (event) {
  const carThumbButton = event.target.closest("[data-car-thumb]");
  if (carThumbButton) {
    const gallery = carThumbButton.closest("[data-car-gallery]");
    const container = gallery?.closest(".panel-card") || document;
    const heroImg = container.querySelector("[data-car-hero-img]");
    const url = carThumbButton.getAttribute("data-car-thumb");
    if (heroImg && url) {
      heroImg.setAttribute("src", url);
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
