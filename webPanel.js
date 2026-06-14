'use strict';

const http   = require('http');
const https  = require('https');
const { CONFIG } = require('../config');

// ── Прокси-запрос к плагину ───────────────────────────────────────────────────
function pluginGet(path) {
    return new Promise((resolve, reject) => {
        const req = http.request({
            hostname: '127.0.0.1',
            port:     CONFIG.MC_API_PORT   || 7654,
            path,
            method:   'GET',
            headers:  { 'X-Secret': CONFIG.MC_API_SECRET || '' },
        }, (res) => {
            let d = '';
            res.on('data', c => d += c);
            res.on('end', () => { try { resolve(JSON.parse(d)); } catch (e) { reject(e); } });
        });
        req.on('error', reject);
        req.setTimeout(4000, () => { req.destroy(); reject(new Error('timeout')); });
        req.end();
    });
}

function pluginPost(path, body) {
    const payload = JSON.stringify({ secret: CONFIG.MC_API_SECRET || '', ...body });
    return new Promise((resolve, reject) => {
        const req = http.request({
            hostname: '127.0.0.1',
            port:     CONFIG.MC_API_PORT || 7654,
            path,
            method:   'POST',
            headers:  { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(payload) },
        }, (res) => {
            let d = '';
            res.on('data', c => d += c);
            res.on('end', () => { try { resolve(JSON.parse(d)); } catch (e) { reject(e); } });
        });
        req.on('error', reject);
        req.setTimeout(4000, () => { req.destroy(); reject(new Error('timeout')); });
        req.write(payload);
        req.end();
    });
}

async function safeGet(path) {
    try { return await pluginGet(path); } catch (_) { return null; }
}

// ── HTML ──────────────────────────────────────────────────────────────────────
const HTML = `<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>DawnGPT Panel</title>
<style>
:root{--bg:#0d0d0d;--s:#1a1a1a;--c:#222;--b:#3a0000;--r:#cc0000;--r2:#ff3333;--r3:#8b0000;--t:#f0f0f0;--m:#888;--g:#00cc66;--y:#ffaa00;}
*{box-sizing:border-box;margin:0;padding:0;}
body{background:var(--bg);color:var(--t);font-family:'Segoe UI',sans-serif;min-height:100vh;display:flex;flex-direction:column;}
header{background:linear-gradient(135deg,#1a0000,#300000,#1a0000);border-bottom:2px solid var(--r);padding:18px 28px;display:flex;align-items:center;gap:16px;}
.logo{font-size:24px;font-weight:900;color:var(--r2);letter-spacing:2px;}
.sub{color:var(--m);font-size:12px;margin-top:2px;}
#upd{margin-left:auto;color:var(--m);font-size:11px;}
nav{background:var(--s);border-bottom:1px solid var(--b);display:flex;padding:0 28px;gap:2px;flex-wrap:wrap;}
nav button{background:none;border:none;border-bottom:2px solid transparent;color:var(--m);padding:13px 16px;cursor:pointer;font-size:13px;transition:.2s;}
nav button.act,nav button:hover{color:var(--r2);border-bottom-color:var(--r2);}
main{flex:1;padding:24px 28px;max-width:1300px;margin:0 auto;width:100%;}
.page{display:none;}.page.act{display:block;}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:14px;margin-bottom:20px;}
.scard{background:var(--c);border:1px solid var(--b);border-radius:10px;padding:18px;text-align:center;}
.scard .v{font-size:34px;font-weight:800;color:var(--r2);}.scard .l{color:var(--m);font-size:11px;margin-top:4px;text-transform:uppercase;letter-spacing:1px;}
.card{background:var(--c);border:1px solid var(--b);border-radius:10px;padding:18px;margin-bottom:14px;}
.card h3{color:var(--r2);font-size:13px;text-transform:uppercase;letter-spacing:1px;margin-bottom:14px;display:flex;align-items:center;gap:8px;}
.card h3 .cnt{background:var(--r3);color:#fff;font-size:11px;padding:1px 7px;border-radius:10px;}
table{width:100%;border-collapse:collapse;font-size:13px;}
th{background:var(--r3);color:#fff;padding:9px 12px;text-align:left;font-weight:600;}
td{padding:9px 12px;border-bottom:1px solid #2a2a2a;vertical-align:middle;}
tr:hover td{background:#2a2a2a;}
.btn{display:inline-flex;align-items:center;gap:5px;padding:6px 14px;border:none;border-radius:6px;cursor:pointer;font-size:12px;font-weight:600;transition:.15s;}
.btn-g{background:#003a1a;color:var(--g);}.btn-g:hover{background:#004d22;}
.btn-r{background:#3a0000;color:var(--r2);}.btn-r:hover{background:#550000;}
.btn-y{background:#3a2a00;color:var(--y);}.btn-y:hover{background:#4d3800;}
.btn-b{background:#1a2a3a;color:#66aaff;}.btn-b:hover{background:#22384f;}
.btn-p{background:var(--r);color:#fff;}.btn-p:hover{background:var(--r2);}
.btn-sm{padding:4px 10px;font-size:11px;}
.badge{display:inline-block;padding:2px 8px;border-radius:4px;font-size:11px;font-weight:600;}
.bg{background:#003a1a;color:var(--g);}.br{background:#3a0000;color:var(--r2);}.by{background:#3a2a00;color:var(--y);}
.dot{display:inline-block;width:8px;height:8px;border-radius:50%;margin-right:5px;}
.dg{background:var(--g);box-shadow:0 0 5px var(--g);}.dr{background:var(--r2);box-shadow:0 0 5px var(--r2);}.dy{background:var(--y);}
.log-box{background:#111;border:1px solid var(--b);border-radius:8px;padding:12px;font-family:monospace;font-size:11px;max-height:380px;overflow-y:auto;color:#ccc;}
.log-l{padding:2px 0;border-bottom:1px solid #1a1a1a;}
input,select{background:var(--s);border:1px solid var(--b);color:var(--t);padding:7px 10px;border-radius:6px;font-size:13px;}
input:focus,select:focus{outline:none;border-color:var(--r);}
.form-row{display:flex;gap:10px;align-items:center;margin-bottom:10px;flex-wrap:wrap;}
.form-row label{color:var(--m);font-size:12px;min-width:120px;}
.pending-card{background:var(--s);border:1px solid var(--b);border-radius:8px;padding:14px;margin-bottom:10px;}
.pending-card .msg{color:var(--t);font-size:14px;margin:6px 0;}
.pending-card .meta{color:var(--m);font-size:11px;}
.pending-card .cmd{background:#111;border:1px solid var(--b);border-radius:4px;padding:6px 10px;font-family:monospace;font-size:12px;color:var(--y);margin:8px 0;}
.pending-card .actions{display:flex;gap:8px;flex-wrap:wrap;margin-top:10px;}
.pending-card input.edit-cmd{flex:1;min-width:200px;}
.bar-bg{background:#333;border-radius:4px;height:8px;overflow:hidden;flex:1;}
.bar-f{height:100%;border-radius:4px;transition:width .5s;}
.trow{display:flex;align-items:center;gap:10px;margin-bottom:8px;}
.tlab{width:55px;color:var(--m);font-size:12px;}
.tnum{width:45px;text-align:right;font-size:12px;font-weight:700;}
.penalty-r{display:flex;gap:12px;padding:8px 0;border-bottom:1px solid #2a2a2a;font-size:13px;}
.penalty-r .rl{color:var(--r2);font-weight:700;min-width:36px;}
.penalty-r .tm{color:var(--y);min-width:100px;text-align:right;}
.toast{position:fixed;bottom:24px;right:24px;background:var(--c);border:1px solid var(--b);color:var(--t);padding:12px 20px;border-radius:8px;font-size:13px;z-index:999;opacity:0;transition:opacity .3s;pointer-events:none;}
.toast.show{opacity:1;}
footer{text-align:center;color:var(--m);padding:20px;font-size:11px;border-top:1px solid var(--b);}
</style>
</head>
<body>
<header>
  <div><div class="logo">⚡ DawnGPT <span style="font-size:20px">🍒</span></div><div class="sub">Панель управления сервером The Dawn · with love from 🍒</div></div>
  <div id="upd"></div>
</header>
<div style="background:linear-gradient(90deg,#7b241c,#c0392b,#e74c3c,#c0392b,#7b241c);height:2px;opacity:0.6"></div>
<div style="background:#1a0000;text-align:center;padding:4px;font-size:11px;color:#7b241c;letter-spacing:3px">🍒 · 🍒 · 🍒</div>
<nav>
  <button class="act" onclick="tab('dashboard',this)">🏠 Дашборд</button>
  <button onclick="tab('pending',this)">⏳ Запросы <span id="nav-pending-cnt" style="color:var(--r2)"></span></button>
  <button onclick="tab('chatctrl',this)">🛡️ Чат-контроль</button>
  <button onclick="tab('patterns',this)">🧩 Паттерны</button>
  <button onclick="tab('server',this)">🖥️ Сервер</button>
  <button onclick="tab('penalties',this)">⚖️ Наказания</button>
  <button onclick="tab('logs',this)">📋 Логи</button>
</nav>
<main>

<!-- ДАШБОРД -->
<div class="page act" id="pg-dashboard">
  <div class="grid" id="dash-grid">
    <div class="scard"><div class="v" id="d-online">—</div><div class="l">Онлайн</div></div>
    <div class="scard"><div class="v" id="d-tps">—</div><div class="l">TPS</div></div>
    <div class="scard"><div class="v" id="d-pending">—</div><div class="l">Ожидают мута</div></div>
    <div class="scard"><div class="v" id="d-patterns">—</div><div class="l">Паттернов</div></div>
    <div class="scard"><div class="v" id="d-chatctrl">—</div><div class="l">ChatControl</div></div>
  </div>
  <div class="card"><h3>Компоненты</h3><div id="d-comp" style="display:flex;flex-direction:column;gap:8px;font-size:13px;"></div></div>
  <div class="card">
    <h3>Запросы на мут <span class="cnt" id="dash-pend-cnt">0</span></h3>
    <div id="dash-pending">Загрузка...</div>
  </div>
</div>

<!-- ЗАПРОСЫ НА МУТ -->
<div class="page" id="pg-pending">
  <div class="card">
    <h3>Ожидающие подтверждения <span class="cnt" id="pend-cnt">0</span></h3>
    <div id="pend-list"><div style="color:var(--m)">Загрузка...</div></div>
  </div>
</div>

<!-- ЧАТ-КОНТРОЛЬ -->
<div class="page" id="pg-chatctrl">
  <div class="card">
    <h3>Настройки ChatControl</h3>
    <div class="form-row">
      <label>ChatControl</label>
      <label style="display:flex;align-items:center;gap:8px;cursor:pointer">
        <input type="checkbox" id="cc-enabled" onchange="setCC('chat-control',this.checked)">
        <span id="cc-enabled-lbl">—</span>
      </label>
    </div>
    <div class="form-row">
      <label>Авто-мут</label>
      <label style="display:flex;align-items:center;gap:8px;cursor:pointer">
        <input type="checkbox" id="cc-automute" onchange="setCC('auto-mute',this.checked)">
        <span id="cc-automute-lbl">—</span>
      </label>
    </div>
    <div class="form-row">
      <label>Чувствительность</label>
      <select id="cc-sens" onchange="setSensitivity(this.value)">
        <option value="low">Низкая (low)</option>
        <option value="medium">Средняя (medium)</option>
        <option value="high">Высокая (high)</option>
      </select>
    </div>
    <div class="form-row">
      <label>Получатели</label>
      <input type="text" id="cc-recv" placeholder="Nick1,Nick2" style="width:260px">
      <button class="btn btn-p btn-sm" onclick="saveReceivers()">💾 Сохранить</button>
    </div>
  </div>
  <div class="card">
    <h3>Статистика</h3>
    <div id="cc-stats" style="color:var(--m);font-size:13px;">Загрузка...</div>
  </div>
</div>

<!-- ПАТТЕРНЫ -->
<div class="page" id="pg-patterns">
  <div class="card">
    <h3>Обученные паттерны <span class="cnt" id="pat-cnt">0</span></h3>
    <div style="display:flex;gap:10px;margin-bottom:14px;flex-wrap:wrap;">
      <input type="text" id="pat-search" placeholder="Поиск..." style="flex:1;min-width:160px" oninput="filterPatterns()">
      <button class="btn btn-r btn-sm" onclick="clearPatterns()">🗑️ Удалить все</button>
    </div>
    <div style="overflow-x:auto;">
      <table>
        <thead><tr><th>#</th><th>Сообщение</th><th>Команда</th><th></th></tr></thead>
        <tbody id="pat-tbody"></tbody>
      </table>
    </div>
    <div style="display:flex;gap:8px;align-items:center;margin-top:12px;">
      <button class="btn btn-b btn-sm" id="pat-prev" onclick="patPage(-1)">◀ Пред.</button>
      <span id="pat-page-info" style="color:var(--m);font-size:12px;flex:1;text-align:center;"></span>
      <button class="btn btn-b btn-sm" id="pat-next" onclick="patPage(1)">След. ▶</button>
    </div>
  </div>
</div>

<!-- СЕРВЕР -->
<div class="page" id="pg-server">
  <div style="display:flex;justify-content:flex-end;margin-bottom:12px;">
    <button class="btn btn-p btn-sm" onclick="loadServer()">🔄 Обновить</button>
  </div>
  <div class="card"><h3>TPS</h3><div id="tps-bars"></div></div>
  <div class="card"><h3>Игроки онлайн</h3><div id="players-list" style="color:var(--m)">Загрузка...</div></div>
</div>

<!-- НАКАЗАНИЯ -->
<div class="page" id="pg-penalties">
  <div class="card">
    <h3>Правила сервера</h3>
    <div id="pen-list"></div>
  </div>
</div>

<!-- ЛОГИ -->
<div class="page" id="pg-logs">
  <div style="display:flex;justify-content:flex-end;margin-bottom:12px;">
    <button class="btn btn-p btn-sm" onclick="loadLogs()">🔄 Обновить</button>
  </div>
  <div class="card"><h3>Логи плагина</h3><div class="log-box" id="log-box">Загрузка...</div></div>
</div>

</main>
<footer>DawnGPT &copy; 2025 — The Dawn Server &nbsp;·&nbsp; made with 🍒</footer>
<div class="toast" id="toast"></div>

<script>
const PENALTIES=[
  {r:'1.1',d:'Флуд / спам / капс',t:'30м – 6ч'},
  {r:'1.2',d:'Оскорбление игроков',t:'1ч – 6ч'},
  {r:'1.3',d:'Оскорбление проекта / администрации',t:'24ч'},
  {r:'1.4',d:'Реклама',t:'3ч / бан 3д'},
  {r:'1.5',d:'Оскорбление родных',t:'12ч / бан 3д'},
  {r:'1.6',d:'Расизм / разжигание розни',t:'12ч'},
  {r:'1.7',d:'Обход мута',t:'6ч'},
  {r:'1.8',d:'Угрозы / выдача за адм',t:'бан 3д'},
  {r:'1.9',d:'Контакты в чате',t:'2ч'},
  {r:'1.10',d:'Признание в нарушении',t:'по факту'},
  {r:'1.11',d:'Аморальная лексика',t:'30м'},
];

let _patPage=0, _patData=[], _currentTab='dashboard';

function tab(name, btn) {
  document.querySelectorAll('.page').forEach(p=>p.classList.remove('act'));
  document.querySelectorAll('nav button').forEach(b=>b.classList.remove('act'));
  document.getElementById('pg-'+name).classList.add('act');
  btn.classList.add('act');
  _currentTab=name;
  if(name==='dashboard') loadDashboard();
  if(name==='pending')   loadPending();
  if(name==='chatctrl')  loadChatCtrl();
  if(name==='patterns')  loadPatterns();
  if(name==='server')    loadServer();
  if(name==='penalties') renderPenalties();
  if(name==='logs')      loadLogs();
}

function toast(msg, ok=true) {
  const t=document.getElementById('toast');
  t.textContent=(ok?'✅ ':'❌ ')+msg;
  t.classList.add('show');
  setTimeout(()=>t.classList.remove('show'),2500);
}

async function api(path,method='GET',body=null) {
  const opts={method,headers:{'Content-Type':'application/json'}};
  if(body) opts.body=JSON.stringify(body);
  const r=await fetch(path,opts);
  return r.json();
}

// ── Дашборд ──────────────────────────────────────────────────────────────────
async function loadDashboard() {
  const [status,tps,pending]=await Promise.all([
    api('/proxy/status').catch(()=>({})),
    api('/proxy/tps').catch(()=>({})),
    api('/proxy/pending').catch(()=>[]),
  ]);
  document.getElementById('d-online').textContent  = tps.onlineCount??'—';
  document.getElementById('d-tps').textContent     = tps.tps1??'—';
  document.getElementById('d-pending').textContent = Array.isArray(pending)?pending.length:(status.pendingCount??'—');
  document.getElementById('d-patterns').textContent= status.learnedCount??'—';
  document.getElementById('d-chatctrl').textContent= status.chatControl?'ВКЛ':'ВЫКЛ';

  const dot=(ok)=>\`<span class="dot \${ok?'dg':'dr'}"></span>\`;
  document.getElementById('d-comp').innerHTML=[
    \`<div>\${dot(status.chatControl)}ChatControl — \${status.chatControl?'ВКЛ':'ВЫКЛ'}</div>\`,
    \`<div>\${dot(status.autoMute)}Авто-мут — \${status.autoMute?'ВКЛ':'ВЫКЛ'}</div>\`,
    \`<div>\${dot((tps.tps1||0)>=15)}Сервер TPS — \${tps.tps1||'н/д'}</div>\`,
    \`<div>\${dot(true)}ИИ (Groq) — подключён</div>\`,
  ].join('');

  const cnt=Array.isArray(pending)?pending.length:0;
  document.getElementById('dash-pend-cnt').textContent=cnt;
  document.getElementById('nav-pending-cnt').textContent=cnt>0?'('+cnt+')':'';

  if(Array.isArray(pending)&&pending.length>0) {
    document.getElementById('dash-pending').innerHTML=pending.slice(0,3).map(renderPendingCard).join('');
  } else {
    document.getElementById('dash-pending').innerHTML='<div style="color:var(--m)">Нет ожидающих запросов</div>';
  }
  document.getElementById('upd').textContent='Обновлено: '+new Date().toLocaleTimeString('ru-RU');
}

// ── Запросы ───────────────────────────────────────────────────────────────────
function renderPendingCard(pm) {
  const age=pm.age<60?pm.age+'с':Math.floor(pm.age/60)+'м назад';
  return \`<div class="pending-card" id="pc-\${pm.id}">
    <div class="meta">🎮 <b>\${pm.player}</b> · \${age} · #\${pm.id}</div>
    <div class="msg">"\${escH(pm.message)}"</div>
    <div class="cmd" id="cmd-\${pm.id}">\${escH(pm.command)}</div>
    <div class="actions">
      <input class="edit-cmd" id="inp-\${pm.id}" type="text" value="\${escH(pm.command)}" placeholder="Команда мута">
      <button class="btn btn-g" onclick="pendingAction('\${pm.id}','confirm')">✔ Подтвердить</button>
      <button class="btn btn-r" onclick="pendingAction('\${pm.id}','reject')">✖ Отклонить</button>
    </div>
  </div>\`;
}

async function loadPending() {
  const data=await api('/proxy/pending').catch(()=>[]);
  document.getElementById('pend-cnt').textContent=data.length;
  document.getElementById('nav-pending-cnt').textContent=data.length>0?\`(\${data.length})\`:'';
  document.getElementById('pend-list').innerHTML=data.length
    ?data.map(renderPendingCard).join('')
    :'<div style="color:var(--m)">Нет ожидающих запросов</div>';
}

async function pendingAction(id, action) {
  const inp=document.getElementById('inp-'+id);
  const command=inp?inp.value.trim():undefined;
  await api('/proxy/pending-action','POST',{id,action,command});
  toast(action==='confirm'?'Мут выдан':'Запрос отклонён', action==='confirm');
  document.getElementById('pc-'+id)?.remove();
  loadDashboard();
}

// ── Чат-контроль ──────────────────────────────────────────────────────────────
async function loadChatCtrl() {
  const s=await api('/proxy/status').catch(()=>({}));
  document.getElementById('cc-enabled').checked=!!s.chatControl;
  document.getElementById('cc-enabled-lbl').textContent=s.chatControl?'ВКЛ':'ВЫКЛ';
  document.getElementById('cc-automute').checked=!!s.autoMute;
  document.getElementById('cc-automute-lbl').textContent=s.autoMute?'ВКЛ':'ВЫКЛ';
  document.getElementById('cc-sens').value=s.sensitivity||'medium';
  document.getElementById('cc-recv').value=(s.receivers||[]).join(',');
  document.getElementById('cc-stats').innerHTML=
    \`Паттернов: <b>\${s.learnedCount||0}</b> · Ожидают: <b>\${s.pendingCount||0}</b> · Получатели: <b>\${(s.receivers||[]).join(', ')||'(нет)'}</b>\`;
}

async function setCC(type, val) {
  const path=type==='chat-control'?'/proxy/set-chat-control':'/proxy/set-auto-mute';
  await api(path,'POST',{enabled:val});
  const lbl=type==='chat-control'?'cc-enabled-lbl':'cc-automute-lbl';
  document.getElementById(lbl).textContent=val?'ВКЛ':'ВЫКЛ';
  toast((type==='chat-control'?'ChatControl':'Авто-мут')+' '+(val?'включён':'выключен'), true);
}

async function setSensitivity(val) {
  await api('/proxy/set-sensitivity','POST',{value:val});
  toast('Чувствительность: '+val, true);
}

async function saveReceivers() {
  const raw=document.getElementById('cc-recv').value;
  const receivers=raw.split(',').map(s=>s.trim()).filter(Boolean);
  await api('/proxy/set-receivers','POST',{receivers});
  toast('Получатели сохранены', true);
}

// ── Паттерны ──────────────────────────────────────────────────────────────────
async function loadPatterns(page=_patPage) {
  _patPage=page;
  const data=await api(\`/proxy/patterns?page=\${page}&size=50\`).catch(()=>({total:0,patterns:[]}));
  _patData=data.patterns||[];
  document.getElementById('pat-cnt').textContent=data.total||0;
  renderPatterns(_patData);
  const maxPage=Math.ceil((data.total||0)/50)-1;
  document.getElementById('pat-page-info').textContent=\`Стр. \${page+1} / \${Math.max(1,maxPage+1)} (всего \${data.total})\`;
  document.getElementById('pat-prev').disabled=page<=0;
  document.getElementById('pat-next').disabled=page>=maxPage;
}

function renderPatterns(patterns) {
  const q=(document.getElementById('pat-search').value||'').toLowerCase();
  const filtered=q?patterns.filter(p=>p.key.includes(q)||p.command.includes(q)):patterns;
  document.getElementById('pat-tbody').innerHTML=filtered.length
    ?filtered.map(p=>\`<tr>
      <td style="color:var(--m)">\${p.index+1}</td>
      <td style="max-width:320px;word-break:break-word">\${escH(p.key)}</td>
      <td style="font-family:monospace;color:var(--y);max-width:260px;word-break:break-word">\${escH(p.command)}</td>
      <td><button class="btn btn-r btn-sm" onclick="deletePattern(\${p.index})">🗑️</button></td>
    </tr>\`).join('')
    :'<tr><td colspan="4" style="color:var(--m);text-align:center">Нет паттернов</td></tr>';
}

function filterPatterns() { renderPatterns(_patData); }
function patPage(d) { loadPatterns(_patPage+d); }

async function deletePattern(idx) {
  await api('/proxy/patterns-delete','POST',{index:idx});
  toast('Паттерн удалён', true);
  loadPatterns(_patPage);
}

async function clearPatterns() {
  if(!confirm('Удалить ВСЕ паттерны?')) return;
  await api('/proxy/patterns-delete','POST',{clear:true});
  toast('Все паттерны удалены', true);
  loadPatterns(0);
}

// ── Сервер ────────────────────────────────────────────────────────────────────
async function loadServer() {
  const [tps,status]=await Promise.all([
    api('/proxy/tps').catch(()=>({})),
    api('/proxy/status').catch(()=>({})),
  ]);
  const bars=['tps1','tps5','tps15'].map((k,i)=>{
    const v=tps[k]||0, pct=Math.min(100,(v/20)*100);
    const col=v>=19?'#00cc66':v>=15?'#ffaa00':'#cc0000';
    const lbl=['1 мин','5 мин','15 мин'][i];
    return \`<div class="trow">
      <div class="tlab">\${lbl}</div>
      <div class="bar-bg"><div class="bar-f" style="width:\${pct}%;background:\${col}"></div></div>
      <div class="tnum" style="color:\${col}">\${v}</div>
    </div>\`;
  }).join('');
  document.getElementById('tps-bars').innerHTML=bars;
  const players=status.onlinePlayers||[];
  document.getElementById('players-list').innerHTML=players.length
    ?players.map(p=>\`<span class="badge bg" style="margin:3px">\${p}</span>\`).join('')
    :'<span style="color:var(--m)">Нет игроков</span>';
}

// ── Наказания ─────────────────────────────────────────────────────────────────
function renderPenalties() {
  document.getElementById('pen-list').innerHTML=PENALTIES.map(p=>
    \`<div class="penalty-r"><div class="rl">\${p.r}</div><div style="flex:1">\${p.d}</div><div class="tm">\${p.t}</div></div>\`
  ).join('');
}

// ── Логи ──────────────────────────────────────────────────────────────────────
async function loadLogs() {
  const data=await api('/proxy/logs').catch(()=>({logs:[]}));
  const lines=(data.logs||[]).slice(-150).reverse();
  document.getElementById('log-box').innerHTML=lines.length
    ?lines.map(l=>\`<div class="log-l">\${escH(l)}</div>\`).join('')
    :'<span style="color:var(--m)">Логов нет</span>';
}

function escH(s){return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');}

// Авто-обновление
loadDashboard();
setInterval(()=>{
  if(_currentTab==='dashboard') loadDashboard();
  if(_currentTab==='pending')   loadPending();
},10000);
</script>
</body>
</html>`;

// ── Прокси-маршруты ───────────────────────────────────────────────────────────
const PROXY_ROUTES = {
    'GET /proxy/status':           () => pluginGet('/api/status'),
    'GET /proxy/tps':              () => pluginGet('/api/tps'),
    'GET /proxy/pending':          () => pluginGet('/api/pending'),
    'GET /proxy/patterns':         (qs) => pluginGet('/api/patterns' + (qs ? '?' + qs : '')),
    'GET /proxy/logs':             () => pluginGet('/api/logs?n=150'),
    'POST /proxy/pending-action':  (_, b) => pluginPost('/api/pending-action', b),
    'POST /proxy/set-chat-control':(_, b) => pluginPost('/api/set-chat-control', b),
    'POST /proxy/set-auto-mute':   (_, b) => pluginPost('/api/set-auto-mute', b),
    'POST /proxy/set-sensitivity': (_, b) => pluginPost('/api/set-sensitivity', b),
    'POST /proxy/set-receivers':   (_, b) => pluginPost('/api/set-receivers', b),
    'POST /proxy/patterns-delete': (_, b) => pluginPost('/api/patterns-delete', b),
};

let _server = null;

function start() {
    const port = CONFIG.WEB_PANEL_PORT || 7656;

    _server = http.createServer(async (req, res) => {
        const [urlPath, qs] = req.url.split('?');
        const method = req.method.toUpperCase();

        // Главная страница
        if (urlPath === '/' || urlPath === '/index.html') {
            res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
            res.end(HTML);
            return;
        }

        // Прокси к плагину
        const routeKey = method + ' ' + urlPath;
        const handler  = PROXY_ROUTES[routeKey];
        if (handler) {
            try {
                let body = null;
                if (method === 'POST') {
                    body = await new Promise((resolve, reject) => {
                        let d = '';
                        req.on('data', c => d += c);
                        req.on('end', () => { try { resolve(JSON.parse(d)); } catch (e) { reject(e); } });
                    });
                }
                const result = await handler(qs, body);
                res.writeHead(200, { 'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*' });
                res.end(JSON.stringify(result));
            } catch (e) {
                res.writeHead(502, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: e.message }));
            }
            return;
        }

        res.writeHead(404).end('Not Found');
    });

    _server.listen(port, '0.0.0.0', () => {
        console.log('[WebPanel] Панель запущена → http://localhost:' + port);
    });
    _server.on('error', e => console.error('[WebPanel] Ошибка:', e.message));
}

function stop() { if (_server) _server.close(); }

module.exports = { start, stop };
