const app = window.app = window.app || {};
Object.assign(app, {
    permissions: new Set(),
});

const loading = app.loadingPanel;

const controls = document.querySelector('panel.controls');
const view = document.querySelector('panel.view');
const panelNothing = view.querySelector(':scope > .show-nothing');
const panelUsers = view.querySelector(':scope > .show-users');
const panelUser = view.querySelector(':scope > .show-user');
const panelEditUser = view.querySelector(':scope > .edit-user');
const panelCreateUser = view.querySelector(':scope > .create-user');
const panelEditDependent = view.querySelector(':scope > .edit-dependent');
const panelCreateDependent = view.querySelector(':scope > .create-dependent');
const panels = [panelNothing, panelUsers, panelUser, panelEditUser, panelCreateUser, panelEditDependent, panelCreateDependent];
panels.hide = (stayPut) => {
    panels.forEach(panel => panel.classList.add('hide'));
    if (!stayPut) window.scrollTo(0, 0);
};

// Set up fetch options
const getOptions = {
    method: 'GET',
    mode: 'same-origin',
    cache: 'reload',
    credentials: 'same-origin',
    headers: {
        'Accept': 'application/json',
    },
};

const postOptions = Object.assign({}, getOptions, {
    method: 'POST',
    headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
    },
});

window.addEventListener('load', async (event) => {
    // Extract permissions and user id from bearer cookie
    (() => {
        const name = 'Bearer=';
        const cookies = document.cookie.split(';');

        let token;
        for (cookie of cookies) {
            cookie = cookie.trim();
            if (!cookie.startsWith(name)) continue;
            token = cookie.substring(name.length, cookie.length);
            break;
        }
        if (!token) return;

        let jwt;
        try {
            jwt = JSON.parse(atob(token.split('.')[1]));
        } catch {
            return;
        }

        const permissionsList = jwt['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        if (permissionsList) {
            if (Array.isArray(permissionsList)) {
                permissionsList.forEach(app.permissions.add, app.permissions);
            }
            else {
                app.permissions.add(permissionsList);
            }
        }

        app.userId = jwt['sub'];
    })();

    // Show additional elements if user has AddEdit role
    if (app.permissions.has('AddEdit')) {
        document.body.classList.add('can-add-edit');
    }
    if (app.permissions.has('ViewOther')) {
        document.body.classList.add('can-view-other');
    }

    const clickHandlers = [
        ['button.home', goHome ],
        ['button.view-user', goShowUser],
        ['button.edit-user', goEditUser],
        ['button.edit-user-submit', goEditUserSubmit],
        ['button.create-user', goCreateUser],
        ['button.create-user-submit', goCreateUserSubmit],
        ['button.edit-dependent', goEditDependent],
        ['button.edit-dependent-submit', goEditDependentSubmit],
        ['button.create-dependent', goCreateDependent],
        ['button.create-dependent-submit', goCreateDependentSubmit],
    ];

    document.body.addEventListener('click', (event) => {
        const target = event.target;

        clickHandlers.some(handler => {
            const button = target.closest(handler[0]);
            return button && handler && !handler[1](button);
        });

    });

    // Update placeholder values in create/edit user panels
    {
        const base = 52000;
        const costSelf = 1000;
        const costDep = 500;
        const modBase = 1;
        const modDiscount = 0.9;

        const handler = ([nameFirst, salaryBase, benefitsCost, modifier, benefitsNet], baseCost, event) => {
            let c = benefitsCost.value;
            if (!c.length) c = baseCost;
            else c = Number.parseFloat(c);
            if (Number.isNaN(c) || !Number.isFinite(c) || c < 0) {
                modifier.placeholder = "Error";
                benefitsNet.value = "Error";
                return;
            }

            const name = nameFirst.value;
            const hasDiscount = !!(name && name.length && name[0] == 'A');

            let m = modifier.value;
            if (!m.length) m = (hasDiscount ? modDiscount : modBase);
            else m = Number.parseFloat(m) / 100 + 1;
            if (Number.isNaN(m) || !Number.isFinite(m) || m < 0) {
                modifier.placeholder = "Error";
                benefitsNet.value = "Error";
                return;
            }

            modifier.placeholder = (m * 100 - 100).toFixed(2);
            benefitsNet.value = (c * m).toFixed(2);
        };

        const update = (args, baseCost) => {
            const [nameFirst, salaryBase, benefitsCost, modifier, benefitsNet] = args;
            benefitsCost.placeholder = baseCost.toFixed(2);
            if (salaryBase) salaryBase.placeholder = base.toFixed(2);
            handler(args, null);
        };


        const fields = [
            'input.name-first',
            'input.salary-base',
            'input.benefits-cost',
            'input.modifier',
            'input.benefits-net',
        ];
        [
            {
                globalFn: 'editUserFieldUpdate',
                panel: panelEditUser,
                isDependent: false,
            },
            {
                globalFn: 'createUserFieldUpdate',
                panel: panelCreateUser,
                isDependent: false,
            },
            {
                globalFn: 'editDependentFieldUpdate',
                panel: panelEditDependent,
                isDependent: true,
            },
            {
                globalFn: 'createDependentFieldUpdate',
                panel: panelCreateDependent,
                isDependent: true,
            },
        ].forEach(({ globalFn, panel, isDependent }) => {
            const el = fields.map(field => panel.querySelector(field));
            const baseCost = isDependent ? costDep : costSelf;
            [el[0], el[2], el[3]].forEach(e => e.addEventListener('input', handler.bind(null, el, baseCost)));
            const upd = update.bind(null, el, baseCost);
            app[globalFn] = upd;
            upd();
        });
    }

    // Start UI
    await goHome();
});

/*********************************************************/

// Go to the user's main view
const goHome = async () => {
    app.loading(true);

    panels.hide();

    if (app.permissions.has('ViewOther')) {
        await showUsers();
    }
    else if (app.permissions.has('ViewSelf')) {
        await showUser();
    }
    else {
        // No useful permissions
        showNothing();
    }

    app.loading(false);
};

// Nothing to see here
const showNothing = () => {
    panelNothing.classList.remove('hide');
};

// Go to the view user panel
const goShowUser = async (button) => {
    app.loading(true);

    panels.hide();
    await showUser(button);

    app.loading(false);
}

// Go to the edit user panel
const goEditUser = async (button) => {
    app.loading(true);

    panels.hide();
    await editUser(button);

    app.loading(false);
};

// Initiate edit user submission
const goEditUserSubmit = async (button) => {
    app.loading(true);

    const results = await editUserSubmit(button);
    if (results) {
        alert(results);
        app.loading(false);
        return;
    }

    goShowUser(button);
};

// Go to the create user panel
const goCreateUser = async (button) => {
    app.loading(true);

    panels.hide();
    await createUser(button);

    app.loading(false);
};

// Initiate create user submission
const goCreateUserSubmit = async (button) => {
    app.loading(true);

    const results = await createUserSubmit(button);
    if (results) {
        alert(results);
        app.loading(false);
        return;
    }

    goShowUser(button);
};

// Go to the edit dependent panel
const goEditDependent = async (button) => {
    app.loading(true);

    panels.hide();
    await editDependent(button);

    app.loading(false);
};

// Initiate dependent edit submission
const goEditDependentSubmit = async (button) => {
    app.loading(true);

    const results = await editDependentSubmit(button);
    if (results) {
        alert(results);
        app.loading(false);
        return;
    }

    goShowUser(button);
};

// Go to the edit dependent panel
const goCreateDependent = async (button) => {
    app.loading(true);

    panels.hide();
    await createDependent(button);

    app.loading(false);
};

// Initiate dependent edit submission
const goCreateDependentSubmit = async (button) => {
    app.loading(true);

    const results = await createDependentSubmit(button);
    if (results) {
        alert(results);
        app.loading(false);
        return;
    }

    goShowUser(button);
};

/*********************************************************/

// Show table of users
const showUsers = async () => {
    let data = fetch('/api/users', getOptions).then(response => response.json());

    const canAddEdit = app.permissions.has('AddEdit');

    const usersPanel = panelUsers.querySelector('.panel');

    const table = usersPanel.querySelector('.users tbody');
    Array.from(table.childNodes).forEach(el => el.remove());

    const buttonView = document.createElement('button');
    buttonView.classList.add('btn', 'btn-primary', 'btn-sm', 'view-user');
    buttonView.textContent = 'View';

    const buttonEdit = document.createElement('button');
    buttonEdit.classList.add('btn', 'btn-primary', 'btn-sm', 'edit-user', 'add-edit');
    buttonEdit.textContent = 'Edit';

    data = await data;

    const fields = [
        d => d.user.id,
        d => d.user.nameFirst,
        d => d.user.nameLast,
        d => `$${d.user.salaryBase.toFixed(2)}`,
        d => `${d.user.dependents ? d.user.dependents.length : 0}`,
        d => `$${d.est.benefitsCost.toFixed(2)}`,
        d => `$${d.est.salaryNet.toFixed(2)}`,
        d => `$${d.est.paycheck.toFixed(2)}`,
    ];

    data.forEach(user => {
        const tr = document.createElement('tr');

        let benefitsCost = user.benefitsCost * user.modifier;
        if ('dependents' in user) {
            user.dependents.forEach(dep => benefitsCost += dep.benefitsCost * dep.modifier);
        }

        user = {
            user,
            est: {
                benefitsCost,
                salaryNet: user.salaryBase - benefitsCost,
                paycheck: (user.salaryBase - benefitsCost) / app.checksPerYear,
            },
        };

        fields.forEach((fn) => {
            const td = document.createElement('td');
            td.textContent = fn(user);
            tr.appendChild(td);
        });

        const td = document.createElement('td');
        let button;

        button = buttonView.cloneNode(true);
        button.dataset['userId'] = user.user.id;
        td.appendChild(button);

        button = buttonEdit.cloneNode(true);
        button.dataset['userId'] = user.user.id;
        if (user.user.id == app.userId || user.user.id == app.superAdminId) button.classList.add('hide');
        td.appendChild(button);

        tr.appendChild(td);

        table.appendChild(tr);
    });

    usersPanel.classList[canAddEdit ? 'add' : 'remove']('add-edit');

    panelUsers.classList.remove('hide');
};

// Show user detail panel
const showUser = async (button) => {
    const userId = button ? button.dataset['userId'] : app.userId;

    const urlGetUser = '/api/user' + (button ? `/${userId}` : '');

    // TODO: handle fetch errors
    let data = fetch(urlGetUser, getOptions).then(response => response.json());

    const userPanel = panelUser.querySelector('.panel');
    const dependents = userPanel.querySelector('.dependents');
    const tbodyDependents = dependents.querySelector('tbody')
    const rowUser = tbodyDependents.querySelector('tr.user');
    const estimate = userPanel.querySelector('.estimate');
    const rowEstimate = estimate.querySelector('tbody > tr.user');

    const buttonCreateDependent = userPanel.querySelector('.create-dependent');
    buttonCreateDependent.dataset['userId'] = userId;

    {
        const rows = tbodyDependents.querySelectorAll('.dependent') || [];
        Array.from(rows).forEach(row => row.remove());
        const button = rowUser.querySelector('button.edit-user');
        if (button) button.remove();
    }

    data = await data;

    // track benefits cost
    let benefitsCost = 0;

    // fill in User values
    [
        ['id', d => d.id],
        ['type', d => 'Self'],
        ['name-first', d => d.nameFirst],
        ['name-last', d => d.nameLast],
        ['benefits-cost', d => `$${d.benefitsCost.toFixed(2)}`],
        ['modifier', d => `${d.modifier < 1 ? '' : '+'}${(d.modifier * 100 - 100).toFixed(2)}%`],
        ['net-cost', d => {
            const netCost = d.benefitsCost * d.modifier;
            benefitsCost += netCost;
            return `$${netCost.toFixed(2)}`;
        }],
    ].forEach(([a, b]) => rowUser.querySelector(`.${a}`).textContent = b(data));
    {
        const button = document.createElement('button');
        button.textContent = 'Edit';
        button.dataset['userId'] = data.id;
        button.classList.add('btn', 'btn-primary', 'btn-sm', 'edit-user', 'add-edit');
        if (data.id == app.userId) button.classList.add('hide');
        const cell = rowUser.querySelector('.add-edit');
        cell.appendChild(button);
        rowUser.appendChild(cell);
    }

    // fill in dependents values
    if ('dependents' in data && data.dependents.length) {
        const fields = [
            ['id', d => d.id],
            ['type', d => 'Dependent'],
            ['name-first', d => d.nameFirst],
            ['name-last', d => d.nameLast],
            ['benefits-cost', d => `$${d.benefitsCost.toFixed(2)}`],
            ['modifier', d => `${d.modifier < 1 ? '' : '+'}${(d.modifier * 100 - 100).toFixed(2)}%`],
            ['net-cost', d => {
                const netCost = d.benefitsCost * d.modifier;
                benefitsCost += netCost;
                return `$${netCost.toFixed(2)}`;
            }],
        ];

        data.dependents.forEach(d => {
            const row = document.createElement('tr');
            row.classList.add('dependent');

            fields.forEach(([a, b]) => {
                const cell = document.createElement('td');
                cell.classList.add(a);
                cell.textContent = b(d);
                row.appendChild(cell);
            });
            {
                const button = document.createElement('button');
                button.textContent = 'Edit';
                button.dataset['userId'] = data.id;
                button.dataset['dependentId'] = d.id;
                button.classList.add('btn', 'btn-primary', 'btn-sm', 'edit-dependent');
                if (data.id == app.userId) button.classList.add('hide');
                const cell = document.createElement('td');
                cell.classList.add('add-edit');
                cell.appendChild(button);
                row.appendChild(cell);
            }

            tbodyDependents.appendChild(row);
        });
    }

    const salaryNet = data.salaryBase - benefitsCost;
    const paycheck = salaryNet / app.checksPerYear;

    [
        ['salary-base', d => `$${d.salaryBase.toFixed(2)}`],
        ['benefits-net', d => `$${benefitsCost.toFixed(2)}`],
        ['salary-net', d => `$${salaryNet.toFixed(2)}`],
        ['paycheck-net', d => `$${paycheck.toFixed(2)}`],
    ].forEach(([a, b]) => rowEstimate.querySelector(`.${a}`).textContent = b(data));

    panelUser.classList.remove('hide');
};

// Show user edit panel
const editUser = async (button) => {
    const userId = button.dataset['userId'];

    const urlGetUser = '/api/user' + (userId ? `/${userId}` : '');

    // TODO: handle fetch errors
    let data = fetch(urlGetUser, getOptions).then(response => response.json());

    const userPanel = panelEditUser.querySelector('.panel');
    const buttonSubmit = userPanel.querySelector('.edit-user-submit');
    const buttonCancel = userPanel.querySelector('.view-user.cancel');

    data = await data;

    // fill in User values
    [
        ['.user-id', d => d.id],
        ['.email', d => d.email],
        ['.name-first', d => d.nameFirst],
        ['.name-last', d => d.nameLast],
        ['.salary-base', d => d.salaryBase.toFixed(2)],
        ['.benefits-cost', d => d.benefitsCost.toFixed(2)],
        ['.modifier', d => `${d.modifier < 1 ? '' : '+'}${(d.modifier * 100 - 100).toFixed(2)}`],
    ].forEach(([a, b]) => {
        const field = userPanel.querySelector(a);
        const value = b(data);
        field.value = value;
        field.setAttribute('value', value);
        field.setAttribute('placeholder', value);
    });

    buttonCancel.dataset['userId'] = userId;
    buttonSubmit.dataset['userId'] = userId;

    app.editUserFieldUpdate();

    panelEditUser.classList.remove('hide');
};

// Handle user edit submit
const editUserSubmit = async (button) => {
    const userPanel = panelEditUser.querySelector('.panel');

    const invalid = userPanel.querySelector(':invalid');
    if (invalid) return "Some fields contain invalid content";

    const userId = button.dataset['userId'];

    const requestData = [
        ['nameFirst', '.name-first', d => d],
        ['nameLast', '.name-last', d => d],
        ['salaryBase', '.salary-base', (d,p) => d ? d : p],
        ['benefitsCost', '.benefits-cost', (d,p) => d ? d : p],
        ['modifier', '.modifier', (d,p) => (d ? d : p) / 100 + 1],
    ].reduce((x, y) => {
        const element = userPanel.querySelector(y[1]);
        const { value, placeholder } = element;
        const original = element.getAttribute('value');
        if (value != original) x.push([y[0], y[2](value, placeholder)]);
        return x;
    }, []);

    if (!requestData.length) return;

    requestData.push(['id', userId]);

    const submission = Object.assign({}, postOptions);
    submission.body = JSON.stringify(Object.fromEntries(requestData));

    const data = await fetch('/api/user', submission).then(response => response.json());

    if ('error' in data) {
        let results = data.error;
        if ('errors' in data) {
            results = data.error + '\n' + data.errors.join('\n');
        }
        return results;
    }
};

// Show create user panel
const createUser = async (button) => {
    const userPanel = panelCreateUser.querySelector('.panel');
    const buttonSubmit = userPanel.querySelector('.create-user-submit');

    ['.email', '.password', '.name-first', '.name-last', '.salary-base', '.benefits-cost', '.modifier', '.benefits-net']
        .forEach(el => userPanel.querySelector(el).value = '');

    app.createUserFieldUpdate();

    panelCreateUser.classList.remove('hide');
};

// Handle create user submit
const createUserSubmit = async (button) => {
    const userPanel = panelCreateUser.querySelector('.panel');

    const invalid = userPanel.querySelector(':invalid');
    if (invalid) return "Some fields contain invalid content";

    const requestData = [
        ['email', '.email', d => d],
        ['password', '.password', d => d],
        ['nameFirst', '.name-first', d => d],
        ['nameLast', '.name-last', d => d],
        ['salaryBase', '.salary-base', (d,p) => d ? d : p],
        ['benefitsCost', '.benefits-cost', (d,p) => d ? d : p],
        ['modifier', '.modifier', (d,p) => (d ? d : p) / 100 + 1],
    ].reduce((x, y) => {
        const element = userPanel.querySelector(y[1]);
        const { value, placeholder } = element;
        const original = element.getAttribute('value');
        if (value != original) x.push([y[0], y[2](value, placeholder)]);
        return x;
    }, []);

    const submission = Object.assign({}, postOptions);
    submission.body = JSON.stringify(Object.fromEntries(requestData));

    const data = await fetch('/api/authenticate/register', submission).then(response => response.json());

    if ('error' in data) {
        let results = data.error;
        if ('errors' in data) {
            results = data.error + '\n' + data.errors.join('\n');
        }
        return results;
    }

    button.dataset['userId'] = data.id;
};

// Dependent edit panel
const editDependent = async (button) => {
    const userId = button.dataset['userId'];
    const depId = button.dataset['dependentId'];

    // TODO: handle fetch errors
    let data = fetch(`/api/dependent/${depId}`, getOptions).then(response => response.json());

    const userPanel = panelEditDependent.querySelector('.panel');
    const buttonSubmit = userPanel.querySelector('.edit-dependent-submit');
    const buttonBack = userPanel.querySelector('.view-user.back');
    const buttonCancel = userPanel.querySelector('.view-user.cancel');

    data = await data;

    // fill in Dependent values
    [
        ['.user-id', d => userId],
        ['.dependent-id', d => depId],
        ['.name-first', d => d.nameFirst],
        ['.name-last', d => d.nameLast],
        ['.benefits-cost', d => d.benefitsCost.toFixed(2)],
        ['.modifier', d => `${d.modifier < 1 ? '' : '+'}${(d.modifier * 100 - 100).toFixed(2)}`],
    ].forEach(([a, b]) => {
        const field = userPanel.querySelector(a);
        const value = b(data);
        field.value = value;
        field.setAttribute('value', value);
        field.setAttribute('placeholder', value);
    });

    buttonBack.dataset['userId'] = userId;
    buttonCancel.dataset['userId'] = userId;
    buttonSubmit.dataset['userId'] = userId;
    buttonSubmit.dataset['dependentId'] = depId;

    app.editDependentFieldUpdate();

    panelEditDependent.classList.remove('hide');
};

// Handle dependent edit submit
const editDependentSubmit = async (button) => {
    const userPanel = panelEditDependent.querySelector('.panel');

    const invalid = userPanel.querySelector(':invalid');
    if (invalid) return "Some fields contain invalid content";

    const userId = button.dataset['userId'];
    const depId = button.dataset['dependentId']

    const qualify = false;

    const requestData = [
        ['nameFirst', '.name-first', d => d],
        ['nameLast', '.name-last', d => d],
        ['benefitsCost', '.benefits-cost', (d, p) => (d ? d : p)],
        ['modifier', '.modifier', (d, p) => (d ? d : p) / 100 + 1],
    ].reduce((x, y) => {
        const element = userPanel.querySelector(y[1])
        const { value, placeholder } = element;
        const original = element.getAttribute('value');
        if (value != original) x.push([y[0], y[2](value, placeholder)]);
        return x;
    }, []);

    if (!requestData.length) return;

    requestData.push(['userId', userId]);
    requestData.push(['id', depId]);

    const submission = Object.assign({}, postOptions);
    submission.body = JSON.stringify(Object.fromEntries(requestData));

    // TODO: handle fetch errors
    const data = await fetch('/api/dependent', submission).then(response => response.json());

    if ('error' in data) {
        let results = data.error;
        if ('errors' in data) {
            results = data.error + '\n' + data.errors.join('\n');
        }
        return results;
    }
};

// Dependent create panel
const createDependent = async (button) => {
    const userId = button.dataset['userId'];

    const userPanel = panelCreateDependent.querySelector('.panel');
    const buttonSubmit = userPanel.querySelector('.create-dependent-submit');
    const buttonBack = userPanel.querySelector('.view-user.back');
    const buttonCancel = userPanel.querySelector('.view-user.cancel');

    ['.name-first', '.name-last', '.benefits-cost', '.modifier', '.benefits-net']
        .forEach(el => userPanel.querySelector(el).value = '');

    userPanel.querySelector('.user-id').value = userId;

    buttonBack.dataset['userId'] = userId;
    buttonCancel.dataset['userId'] = userId;
    buttonSubmit.dataset['userId'] = userId;

    app.createDependentFieldUpdate();

    panelCreateDependent.classList.remove('hide');
};

// Handle dependent create submit
const createDependentSubmit = async (button) => {
    const userPanel = panelCreateDependent.querySelector('.panel');

    const invalid = userPanel.querySelector(':invalid');
    if (invalid) return "Some fields contain invalid content";

    const userId = button.dataset['userId'];
    //const depId = button.dataset['dependentId']

    const requestData = [
        ['nameFirst', '.name-first', d => d],
        ['nameLast', '.name-last', d => d],
        ['benefitsCost', '.benefits-cost', (d,p) => d ? d : p],
        ['modifier', '.modifier', (d,p) => (d ? d : p) / 100 + 1],
    ].reduce((x, y) => {
        const element = userPanel.querySelector(y[1])
        const { value, dependent } = element;
        const original = element.getAttribute('value');
        if (value != original) x.push([y[0], y[2](value, dependent)]);
        return x;
    }, []);

    if (!requestData.length) return;

    requestData.push(['userId', userId]);
    //requestData.push(['id', depId]);

    const submission = Object.assign({}, postOptions);
    submission.body = JSON.stringify(Object.fromEntries(requestData));

    // TODO: handle fetch errors
    const data = await fetch('/api/dependent', submission).then(response => response.json());

    if ('error' in data) {
        let results = data.error;
        if ('errors' in data) {
            results = data.error + '\n' + data.errors.join('\n');
        }
        return results;
    }
};

