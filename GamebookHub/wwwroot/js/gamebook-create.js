/* ============================================
   GAMEBOOK CREATE FORM - FUNCTIONALITY
   ============================================ */

(function () {
    'use strict';

    // ====================================
    // DOM ELEMENTS
    // ====================================
    const csToggle = document.getElementById('cs-enabled');
    const csBlock = document.getElementById('cs-block');
    const invToggle = document.getElementById('inv-enabled');
    const invBlock = document.getElementById('inv-block');
    const invMode = document.getElementById('inv-mode');
    const addBtn = document.getElementById('add-attribute');
    const list = document.getElementById('attributes-list');

    // ====================================
    // CHARACTER SHEET TOGGLE
    // ====================================
    if (csToggle && csBlock) {
        csToggle.addEventListener('change', () => {
            csBlock.style.display = csToggle.checked ? 'block' : 'none';
        });
    }

    // ====================================
    // INVENTORY TOGGLE
    // ====================================
    if (invToggle && invBlock) {
        invToggle.addEventListener('change', () => {
            invBlock.style.display = invToggle.checked ? 'block' : 'none';
        });
    }

    // ====================================
    // INVENTORY MODE CHANGE
    // ====================================
    if (invMode) {
        const updateInvFields = () => {
            const mode = invMode.value;
            const slotsField = document.querySelector('.inv-slots');
            const capacityField = document.querySelector('.inv-capacity');
            
            if (slotsField) slotsField.style.display = mode === 'Slots' ? 'block' : 'none';
            if (capacityField) capacityField.style.display = mode === 'Weight' ? 'block' : 'none';
        };

        invMode.addEventListener('change', updateInvFields);
        updateInvFields();
    }

    // ====================================
    // DYNAMIC ATTRIBUTES
    // ====================================
    let index = document.querySelectorAll('#attributes-list .attribute-card').length || 0;

    const attributeTemplate = (i) => `
<div class="attribute-card" data-index="${i}">
    <input type="hidden" name="CharacterSheet.Attributes.Index" value="${i}" />
    <input type="hidden" name="CharacterSheet.Attributes[${i}].Id" value="0" />

    <div class="row g-2 align-items-end">
        <div class="col-md-4">
            <label class="form-label small">Label</label>
            <input class="form-control attr-label" name="CharacterSheet.Attributes[${i}].Label" placeholder="Ex: Força" />
        </div>
        <div class="col-md-4">
            <label class="form-label small">Key</label>
            <input class="form-control attr-key" name="CharacterSheet.Attributes[${i}].Key" placeholder="forca" />
        </div>
        <div class="col-md-4">
            <label class="form-label small">Tipo</label>
            <select class="form-select attr-type" name="CharacterSheet.Attributes[${i}].Type">
                <option value="">Selecione...</option>
                <option value="Integer">Inteiro</option>
                <option value="Decimal">Decimal</option>
                <option value="Boolean">Booleano</option>
                <option value="Text">Texto</option>
                <option value="Enum">Enumeração</option>
                <option value="Resource">Recurso</option>
            </select>
        </div>

        <div class="col-md-3">
            <label class="form-label small">Mínimo</label>
            <input class="form-control" type="number" name="CharacterSheet.Attributes[${i}].Min" placeholder="0" />
        </div>
        <div class="col-md-3">
            <label class="form-label small">Máximo</label>
            <input class="form-control" type="number" name="CharacterSheet.Attributes[${i}].Max" placeholder="100" />
        </div>
        <div class="col-md-3">
            <label class="form-label small">Padrão</label>
            <input class="form-control" type="number" name="CharacterSheet.Attributes[${i}].Default" placeholder="50" />
        </div>

        <div class="col-md-6">
            <label class="form-label small">Opções (separadas por vírgula)</label>
            <input class="form-control enum-options" name="CharacterSheet.Attributes[${i}].EnumOptions" placeholder="Opção1,Opção2,Opção3" />
        </div>

        <div class="col-md-3">
            <div class="form-check">
                <input type="hidden" name="CharacterSheet.Attributes[${i}].Visible" value="false" />
                <input class="form-check-input" type="checkbox" id="visible-${i}"
                       name="CharacterSheet.Attributes[${i}].Visible" value="true" checked />
                <label class="form-check-label small" for="visible-${i}">Visível</label>
            </div>
        </div>

        <div class="col-md-2">
            <label class="form-label small">Ordem</label>
            <input class="form-control" type="number" name="CharacterSheet.Attributes[${i}].Order" value="${i + 1}" />
        </div>

        <div class="col-md-1 text-end">
            <button type="button" class="btn btn-sm btn-danger-outline remove-attribute" title="Remover atributo">
                <i class="bi bi-trash"></i>
            </button>
        </div>
    </div>
</div>`;

    // ====================================
    // SLUGIFY HELPER
    // ====================================
    const slugify = (str) => (str || '')
        .toString()
        .trim()
        .toLowerCase()
        .normalize('NFD')
        .replace(/\p{Diacritic}/gu, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/(^-|-$)/g, '');

    // ====================================
    // WIRE ATTRIBUTE
    // ====================================
    const wireAttribute = (item) => {
        const label = item.querySelector('.attr-label');
        const key = item.querySelector('.attr-key');
        
        if (label && key) {
            label.addEventListener('input', () => {
                if (!key.value || key.value === slugify(key.value)) {
                    key.value = slugify(label.value);
                }
            });
        }
    };

    // ====================================
    // ADD ATTRIBUTE
    // ====================================
    if (addBtn && list) {
        addBtn.addEventListener('click', (e) => {
            e.preventDefault();
            const wrapper = document.createElement('div');
            wrapper.innerHTML = attributeTemplate(index++);
            const node = wrapper.firstElementChild;
            list.appendChild(node);
            wireAttribute(node);
            
            // Scroll to new element
            node.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        });
    }

    // ====================================
    // REMOVE ATTRIBUTE
    // ====================================
    if (list) {
        list.addEventListener('click', (e) => {
            const btn = e.target.closest('.remove-attribute');
            if (btn) {
                const item = btn.closest('.attribute-card');
                if (item) {
                    item.remove();
                }
            }
        });
    }

    // ====================================
    // INITIALIZE EXISTING ATTRIBUTES
    // ====================================
    document.querySelectorAll('.attribute-card').forEach(wireAttribute);

    // ====================================
    // SHOW VALIDATION ERRORS
    // ====================================
    const validationAlert = document.getElementById('validationAlert');
    if (validationAlert) {
        const errors = validationAlert.querySelectorAll('li');
        if (errors.length > 0) {
            validationAlert.style.display = 'block';
        }
    }

})();
