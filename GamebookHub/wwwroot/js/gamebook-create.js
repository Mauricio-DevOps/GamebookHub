(function () {
    const csToggle = document.getElementById('cs-enabled');
    const csBlock = document.getElementById('cs-block');
    const invToggle = document.getElementById('inv-enabled');
    const invBlock = document.getElementById('inv-block');
    const invMode = document.getElementById('inv-mode');

    // Mostrar/ocultar ficha
    if (csToggle && csBlock) {
        csToggle.addEventListener('change', () => {
            csBlock.style.display = csToggle.checked ? 'block' : 'none';
        });
    }

    // Mostrar/ocultar inventário
    if (invToggle && invBlock) {
        invToggle.addEventListener('change', () => {
            invBlock.style.display = invToggle.checked ? 'block' : 'none';
        });
    }

    // Ajustar campos por modo de inventário
    if (invMode) {
        const updateInvFields = () => {
            const mode = invMode.options[invMode.selectedIndex].text;
            document.querySelectorAll('.inv-field').forEach(el => el.style.display = 'none');
            const slots = document.querySelector('.inv-slots');
            const capacity = document.querySelector('.inv-capacity');
            if (mode === 'Slots' && slots) slots.style.display = 'block';
            if (mode === 'Weight' && capacity) capacity.style.display = 'block';
        };
        invMode.addEventListener('change', updateInvFields);
        updateInvFields();
    }

    // ====== Lista dinâmica de Atributos ======
    let index = document.querySelectorAll('#attributes-list .attribute-item').length || 0;
    const list = document.getElementById('attributes-list');
    const addBtn = document.getElementById('add-attribute');

    const template = (i) => `
<div class="border rounded p-3 mb-3 attribute-item" data-index="${i}">
  <!-- necessários para binding da coleção -->
  <input type="hidden" name="CharacterSheet.Attributes.Index" value="${i}" />
  <input type="hidden" name="CharacterSheet.Attributes[${i}].Id" value="0" />

  <div class="row g-3">
    <div class="col-md-4">
      <label class="form-label">Label</label>
      <input class="form-control attr-label" name="CharacterSheet.Attributes[${i}].Label" />
    </div>
    <div class="col-md-4">
      <label class="form-label">Key</label>
      <input class="form-control attr-key" name="CharacterSheet.Attributes[${i}].Key" />
    </div>
    <div class="col-md-4">
      <label class="form-label">Tipo</label>
      <select class="form-select attr-type" name="CharacterSheet.Attributes[${i}].Type">
        <option>Integer</option>
        <option>Decimal</option>
        <option>Boolean</option>
        <option>Text</option>
        <option>Enum</option>
        <option>Resource</option>
      </select>
    </div>

    <div class="col-md-4">
      <label class="form-label">Min</label>
      <input class="form-control" name="CharacterSheet.Attributes[${i}].Min" />
    </div>
    <div class="col-md-4">
      <label class="form-label">Max</label>
      <input class="form-control" name="CharacterSheet.Attributes[${i}].Max" />
    </div>
    <div class="col-md-4">
      <label class="form-label">Default</label>
      <input class="form-control" name="CharacterSheet.Attributes[${i}].Default" />
    </div>

    <div class="col-md-6">
      <label class="form-label">Enum Options (vírgula)</label>
      <input class="form-control enum-options" name="CharacterSheet.Attributes[${i}].EnumOptions" />
    </div>

    <div class="col-md-3 d-flex align-items-end">
      <div class="form-check">
        <input type="hidden" name="CharacterSheet.Attributes[${i}].Visible" value="false" />
        <input class="form-check-input"
               type="checkbox"
               name="CharacterSheet.Attributes[${i}].Visible"
               value="true"
               checked />
        <label class="form-check-label">Visível</label>
      </div>
    </div>

    <div class="col-md-3">
      <label class="form-label">Ordem</label>
      <input class="form-control" type="number" name="CharacterSheet.Attributes[${i}].Order" value="${i + 1}" />
    </div>
  </div>

  <div class="mt-3 text-end">
    <button type="button" class="btn btn-outline-danger btn-sm remove-attribute">Remover</button>
  </div>
</div>`;


    const slugify = (str) => (str || '').toString().trim()
        .toLowerCase()
        .normalize('NFD').replace(/\p{Diacritic}/gu, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/(^-|-$)/g, '');

    const wireAttribute = (item) => {
        const label = item.querySelector('.attr-label');
        const key = item.querySelector('.attr-key');
        if (label && key) {
            label.addEventListener('input', () => {
                if (!key.value) key.value = slugify(label.value);
            });
        }
    };

    addBtn?.addEventListener('click', () => {
        const wrapper = document.createElement('div');
        wrapper.innerHTML = template(index++);
        const node = wrapper.firstElementChild;
        list.appendChild(node);
        wireAttribute(node);
    });

    list?.addEventListener('click', (e) => {
        const btn = e.target.closest('.remove-attribute');
        if (btn) {
            btn.closest('.attribute-item')?.remove();
        }
    });

    // Wire rows existentes (renderizadas pelo servidor)
    document.querySelectorAll('.attribute-item').forEach(wireAttribute);
})();
