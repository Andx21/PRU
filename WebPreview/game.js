/* ============================================================================
 * Tower Defense Lite — bản chơi thử trên trình duyệt
 *
 * ĐÂY KHÔNG PHẢI BÀI NỘP. Bài nộp là project Unity ở thư mục Assets/.
 * File này chỉ để chạy thử ngay trên Chrome khi chưa cài Unity: dùng ĐÚNG bộ
 * art, ĐÚNG toạ độ waypoint lấy từ scene Test.unity, và ĐÚNG các con số cân
 * bằng trong TowerDefenseAssetSetup.cs. Chỉnh số ở đây thấy hợp lý thì chép
 * y nguyên sang asset Unity.
 * ==========================================================================*/

// ── Thông số: giữ khớp với TowerDefenseAssetSetup.cs ────────────────────────

const START_LIVES = 20;
const START_COINS = 150;

const ENEMY_TYPES = {
  slime: { name: 'Slime', hp: 40,  speed: 1.8, coin: 8,   dmg: 1,
           sheet: 'Slime_Walk', cellW: 96, cellH: 96, frames: 8, ppu: 100, scale: 3 },
  orc:   { name: 'Orc',   hp: 80,  speed: 1.4, coin: 14,  dmg: 2,
           sheet: 'Orc_Walk',   cellW: 100, cellH: 100, frames: 8, ppu: 100, scale: 4 },
  boss:  { name: 'Boss',  hp: 600, speed: 1.0, coin: 100, dmg: 10, isBoss: true,
           sheet: 'Boss_Walk-Sheet', cellW: 260, cellH: 284, frames: 11, ppu: 300, scale: 1,
           pivotBottom: true }
};

const TOWER_TYPES = [
  { id: 'archer', name: 'Cung thủ', attack: 'ranged',
    dmg: 12, range: 3.2, rate: 1.4, cost: 50,
    img: 'Tower_Archer', ppu: 700, proj: 'Arrow', projPpu: 2600, projSpeed: 11,
    info: 'Tầm xa · 1 mục tiêu' },

  { id: 'melee', name: 'Lính cận chiến', attack: 'melee',
    dmg: 25, range: 1.4, rate: 2.0, cost: 75,
    img: 'Tower_Barracks', ppu: 700,
    info: 'Tầm gần · trúng ngay' },

  { id: 'cannon', name: 'Pháo', attack: 'splash',
    dmg: 18, range: 2.8, rate: 0.7, cost: 100, splash: 1.3,
    img: 'Tower_Cannon', ppu: 700, proj: 'Cannonball', projPpu: 2600, projSpeed: 7,
    info: 'Nổ lan · sát thương cụm' }
];

const WAVES = [
  { name: 'Đợt 1 — dò đường', delay: 3,  entries: [{ type: 'slime', count: 6,  gap: 0.9 }] },
  { name: 'Đợt 2 — đông hơn', delay: 8,  entries: [{ type: 'slime', count: 10, gap: 0.7 }] },
  { name: 'Đợt 3 — có Orc',   delay: 10, entries: [{ type: 'slime', count: 6,  gap: 0.6 },
                                                    { type: 'orc',   count: 5,  gap: 1.0 }] },
  { name: 'Đợt 4 — Orc tràn', delay: 12, entries: [{ type: 'orc',   count: 12, gap: 0.7 }] },
  { name: 'Đợt 5 — BOSS',     delay: 14, entries: [{ type: 'orc',   count: 8,  gap: 0.6 },
                                                    { type: 'boss',  count: 1,  gap: 1.0 }] }
];

const DEATH_FADE = 0.6;      // giây, khớp Enemy.deathFadeDuration
const DEATH_SINK = 0.15;     // unit
const SLOT_CLICK_RADIUS = 0.55;
const MAP_PPU = 100;

// Ảnh map 1908x858 px ở PPU 100 -> 19.08 x 8.58 unit.
// Scene Unity đang để camera ortho size 5, tức nhìn thấy 10 unit chiều cao,
// cao hơn map 1.42 unit nên hở viền đen trên và dưới. Đặt đúng 8.58/2 = 4.29
// thì map vừa khít khung hình. NHỚ SỬA CẢ TRONG UNITY: Main Camera > Size = 4.29
const CAMERA_HEIGHT_UNITS = 8.58;

// ── Tài nguyên ──────────────────────────────────────────────────────────────

const IMG = {};
const IMAGE_FILES = [
  'Map.jpg', 'Tower_Slot.png', 'Tower_Archer.png', 'Tower_Barracks.png', 'Tower_Cannon.png',
  'Arrow.png', 'Cannonball.png', 'Slime_Walk.png', 'Orc_Walk.png', 'Boss_Walk-Sheet.png'
];

function loadAll() {
  return Promise.all([
    fetch('assets/scene.json').then(r => r.json()),
    ...IMAGE_FILES.map(f => new Promise((res, rej) => {
      const im = new Image();
      im.onload = () => { IMG[f.replace(/\.(png|jpg)$/, '')] = im; res(); };
      im.onerror = () => rej(new Error('Không tải được assets/' + f));
      im.src = 'assets/' + f;
    }))
  ]).then(([scene]) => scene);
}

// ── Âm thanh: tự tổng hợp bằng WebAudio, không cần file ────────────────────

const Audio2 = {
  ctx: null, music: 0.4, sfx: 0.7, musicNode: null,

  ensure() {
    if (!this.ctx) this.ctx = new (window.AudioContext || window.webkitAudioContext)();
    if (this.ctx.state === 'suspended') this.ctx.resume();
  },

  blip(freq, dur, type = 'square', gain = 0.25) {
    if (!this.ctx || this.sfx <= 0) return;
    const o = this.ctx.createOscillator(), g = this.ctx.createGain();
    o.type = type; o.frequency.value = freq;
    g.gain.setValueAtTime(gain * this.sfx, this.ctx.currentTime);
    g.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + dur);
    o.connect(g).connect(this.ctx.destination);
    o.start(); o.stop(this.ctx.currentTime + dur);
  },

  shoot()  { this.blip(680, 0.05, 'square', 0.12); },
  death()  { this.blip(180, 0.18, 'sawtooth', 0.20); },
  build()  { this.blip(440, 0.10, 'triangle', 0.30); this.blip(660, 0.10, 'triangle', 0.22); },
  hurt()   { this.blip(120, 0.30, 'sawtooth', 0.35); },
  error()  { this.blip(140, 0.12, 'square', 0.25); },
  win()    { [523, 659, 784, 1046].forEach((f, i) => setTimeout(() => this.blip(f, 0.25, 'triangle', 0.3), i * 130)); },
  lose()   { [392, 330, 262, 196].forEach((f, i) => setTimeout(() => this.blip(f, 0.35, 'sawtooth', 0.3), i * 180)); },

  // "Nhạc nền": một nốt trầm lặp lại, chỉ để có cái điều chỉnh âm lượng nhạc.
  startMusic() {
    if (!this.ctx || this.musicNode) return;
    const o = this.ctx.createOscillator(), g = this.ctx.createGain(), lfo = this.ctx.createOscillator(), lg = this.ctx.createGain();
    o.type = 'sine'; o.frequency.value = 98;
    lfo.frequency.value = 0.22; lg.gain.value = 0.02;
    g.gain.value = this.music * 0.05;
    lfo.connect(lg).connect(g.gain);
    o.connect(g).connect(this.ctx.destination);
    o.start(); lfo.start();
    this.musicNode = g;
  },

  setMusic(v) { this.music = v; if (this.musicNode) this.musicNode.gain.value = v * 0.05; },
  setSfx(v)   { this.sfx = v; }
};

// ── Pool: y hệt GameObjectPool.cs, giữ trong một mảng và bật/tắt cờ active ──

class Pool {
  constructor(factory) { this.factory = factory; this.items = []; }

  get() {
    for (const it of this.items) if (!it.active) return it;
    const fresh = this.factory();
    this.items.push(fresh);
    return fresh;
  }

  get activeItems() { return this.items.filter(i => i.active); }
  returnAll() { for (const it of this.items) it.active = false; }
}

// ── Trạng thái ván chơi ─────────────────────────────────────────────────────

const Game = {
  state: 'playing',        // playing | paused | won | lost
  lives: START_LIVES,
  coins: START_COINS,
  wave: 0,
  path: [], slots: [],
  enemyPools: {}, bulletPool: null,
  selected: null,
  hoverSlot: null,
  mouse: { x: 0, y: 0, wx: 0, wy: 0 }
};

let cv, ctx, PXU = 100, scene;

// ── Đổi toạ độ thế giới <-> màn hình (giống camera Unity) ──────────────────

const toScreenX = wx => cv.width / 2 + wx * PXU;
const toScreenY = wy => cv.height / 2 - wy * PXU;
const toWorldX  = sx => (sx - cv.width / 2) / PXU;
const toWorldY  = sy => (cv.height / 2 - sy) / PXU;

function resize() {
  // Giữ khung 16:9 vừa cửa sổ, chừa chỗ cho shop.
  const maxW = window.innerWidth - 24, maxH = window.innerHeight - 24;
  let w = maxW, h = w * 9 / 16;
  if (h > maxH) { h = maxH; w = h * 16 / 9; }
  cv.width = Math.round(w); cv.height = Math.round(h);
  PXU = cv.height / CAMERA_HEIGHT_UNITS;

  const stage = document.getElementById('stage');
  stage.style.width = cv.width + 'px';

  // UI co giãn theo bề ngang khung hình, không thì mở cửa sổ nhỏ là chữ và thẻ
  // shop phình to đè lên nhau.
  stage.style.setProperty('--ui', (cv.width / 1280).toFixed(3));
}

// ── Quái ───────────────────────────────────────────────────────────────────

function makeEnemy(typeKey) {
  return {
    active: false, typeKey, def: ENEMY_TYPES[typeKey],
    x: 0, y: 0, wpIndex: 0, hp: 0, dying: false, deathT: 0,
    flip: false, animT: 0, progress: 0
  };
}

function spawnEnemy(typeKey) {
  const e = Game.enemyPools[typeKey].get();
  const d = ENEMY_TYPES[typeKey];

  // Reset TOÀN BỘ trạng thái — đối tượng này là đồ tái sử dụng từ pool.
  e.active = true;
  e.x = Game.path[0][0]; e.y = Game.path[0][1];
  e.wpIndex = 0; e.hp = d.hp; e.dying = false; e.deathT = 0;
  e.animT = 0; e.progress = 0; e.flip = false;
}

function updateEnemy(e, dt) {
  if (e.dying) {
    e.deathT += dt;
    if (e.deathT >= DEATH_FADE) e.active = false;
    return;
  }

  e.animT += dt;

  const target = Game.path[e.wpIndex];
  if (!target) return;

  const dx = target[0] - e.x, dy = target[1] - e.y;
  const dist = Math.hypot(dx, dy);
  const step = e.def.speed * dt;

  if (Math.abs(dx) > 0.01) e.flip = dx < 0;

  if (dist <= step) {
    e.x = target[0]; e.y = target[1];
    e.wpIndex++;
    if (e.wpIndex >= Game.path.length) reachBase(e);
  } else {
    e.x += dx / dist * step;
    e.y += dy / dist * step;
  }

  e.progress = e.wpIndex + (1 - Math.min(1, dist));
}

function reachBase(e) {
  e.active = false;
  Game.lives = Math.max(0, Game.lives - e.def.dmg);
  Audio2.hurt();
  refreshHud();
  if (Game.lives === 0) endGame(false);
}

function damageEnemy(e, amount) {
  if (e.dying || !e.active) return;
  e.hp -= amount;
  if (e.hp <= 0) {
    e.dying = true; e.deathT = 0;
    Game.coins += e.def.coin;
    Audio2.death();
    refreshHud();
  }
}

function allEnemies() {
  let out = [];
  for (const k in Game.enemyPools) out = out.concat(Game.enemyPools[k].activeItems);
  return out;
}

// ── Tháp ───────────────────────────────────────────────────────────────────

function buildTower(slot, def) {
  if (slot.tower) { Audio2.error(); return false; }
  if (Game.coins < def.cost) { Audio2.error(); return false; }

  Game.coins -= def.cost;
  slot.tower = { def, cooldown: 0 };
  Audio2.build();

  // Xây được cái đầu tiên là người chơi đã hiểu cách chơi, cất dòng gợi ý đi.
  const hint = document.getElementById('hint');
  if (hint) hint.style.display = 'none';
  refreshHud();
  Game.selected = null;
  renderShop();
  return true;
}

function updateTower(slot, dt) {
  const t = slot.tower;
  if (!t) return;

  t.cooldown -= dt;

  // Ngắm con đi xa nhất trên đường mà còn trong tầm — con gần Base nhất.
  let best = null, bestProgress = -1;
  for (const e of allEnemies()) {
    if (e.dying) continue;
    if (Math.hypot(e.x - slot.x, e.y - slot.y) > t.def.range) continue;
    if (e.progress > bestProgress) { bestProgress = e.progress; best = e; }
  }
  if (!best) return;

  if (t.cooldown <= 0) {
    t.cooldown = 1 / t.def.rate;
    Audio2.shoot();

    if (t.def.attack === 'melee') {
      damageEnemy(best, t.def.dmg);
    } else {
      const b = Game.bulletPool.get();
      b.active = true;
      b.x = slot.x; b.y = slot.y + 0.35;
      b.target = best; b.def = t.def; b.life = 0;
      b.lastX = best.x; b.lastY = best.y;
    }
  }
}

function updateBullet(b, dt) {
  b.life += dt;
  if (b.life > 3) { b.active = false; return; }

  if (b.target && b.target.active && !b.target.dying) {
    b.lastX = b.target.x; b.lastY = b.target.y;
  }

  const dx = b.lastX - b.x, dy = b.lastY - b.y;
  const dist = Math.hypot(dx, dy);
  const step = b.def.projSpeed * dt;

  if (dist <= step) {
    if (b.def.attack === 'splash') {
      for (const e of allEnemies()) {
        if (Math.hypot(e.x - b.lastX, e.y - b.lastY) <= b.def.splash) damageEnemy(e, b.def.dmg);
      }
    } else if (b.target) {
      damageEnemy(b.target, b.def.dmg);
    }
    b.active = false;
    return;
  }

  b.x += dx / dist * step;
  b.y += dy / dist * step;
  b.angle = Math.atan2(dy, dx);
}

// ── Điều phối đợt ──────────────────────────────────────────────────────────

async function runWaves() {
  for (let i = 0; i < WAVES.length; i++) {
    const w = WAVES[i];
    await wait(w.delay);
    Game.wave = i + 1;
    refreshHud();

    for (const entry of w.entries) {
      for (let n = 0; n < entry.count; n++) {
        spawnEnemy(entry.type);
        await wait(entry.gap);
      }
    }
  }

  // Hết đợt cuối vẫn phải dọn sạch quái còn sống mới tính thắng.
  while (allEnemies().length > 0) await wait(0.2);
  endGame(true);
}

/** Chờ theo giờ GAME: đang tạm dừng hoặc đã kết thúc thì đồng hồ đứng luôn. */
function wait(seconds) {
  return new Promise(resolve => {
    let left = seconds;
    const tick = () => {
      if (Game.state === 'won' || Game.state === 'lost') return;   // bỏ luôn, không resolve
      if (Game.state === 'playing') left -= 1 / 60;
      if (left <= 0) resolve(); else requestAnimationFrame(tick);
    };
    tick();
  });
}

function endGame(won) {
  if (Game.state === 'won' || Game.state === 'lost') return;
  Game.state = won ? 'won' : 'lost';

  document.getElementById('endTitle').textContent = won ? 'CHIẾN THẮNG' : 'THẤT BẠI';
  document.getElementById('endTitle').style.color = won ? '#7ee08a' : '#f26a6a';
  document.getElementById('endMsg').textContent = won
    ? 'Bạn đã chặn được toàn bộ 5 đợt tấn công.'
    : `Base thất thủ ở đợt ${Game.wave}/${WAVES.length}.`;
  document.getElementById('endOv').classList.add('on');

  won ? Audio2.win() : Audio2.lose();
}

// ── Vẽ ─────────────────────────────────────────────────────────────────────

function drawSprite(img, wx, wy, wUnits, hUnits, opt = {}) {
  const w = wUnits * PXU, h = hUnits * PXU;
  const x = toScreenX(wx), y = toScreenY(wy);
  const oy = opt.bottom ? y - h : y - h / 2;

  ctx.save();
  if (opt.alpha !== undefined) ctx.globalAlpha = opt.alpha;

  if (opt.angle) {
    ctx.translate(x, y); ctx.rotate(-opt.angle);
    ctx.drawImage(img, -w / 2, -h / 2, w, h);
  } else if (opt.flip) {
    ctx.translate(x, 0); ctx.scale(-1, 1);
    ctx.drawImage(img, -w / 2, oy, w, h);
  } else if (opt.sx !== undefined) {
    ctx.drawImage(img, opt.sx, opt.sy, opt.sw, opt.sh, x - w / 2, oy, w, h);
  } else {
    ctx.drawImage(img, x - w / 2, oy, w, h);
  }
  ctx.restore();
}

function drawEnemy(e) {
  const d = e.def;
  const frame = Math.floor(e.animT * 10) % d.frames;
  const wUnits = d.cellW / d.ppu * d.scale;
  const hUnits = d.cellH / d.ppu * d.scale;
  const alpha = e.dying ? 1 - e.deathT / DEATH_FADE : 1;
  const sink = e.dying ? DEATH_SINK * (e.deathT / DEATH_FADE) : 0;

  const img = IMG[d.sheet];
  const sx = frame * d.cellW;
  const x = toScreenX(e.x), y = toScreenY(e.y - sink);
  const w = wUnits * PXU, h = hUnits * PXU;
  const oy = d.pivotBottom ? y - h : y - h / 2;

  ctx.save();
  ctx.globalAlpha = alpha;
  if (e.flip) {
    ctx.translate(x * 2, 0); ctx.scale(-1, 1);
  }
  ctx.drawImage(img, sx, 0, d.cellW, d.cellH, x - w / 2, oy, w, h);
  ctx.restore();

  // Thanh máu
  if (!e.dying && e.hp < d.hp) {
    const bw = (d.isBoss ? 1.1 : 0.55) * PXU, bh = 5;
    const by = (d.pivotBottom ? oy : y - h / 2) - 8;
    ctx.fillStyle = '#000a'; ctx.fillRect(x - bw / 2, by, bw, bh);
    ctx.fillStyle = d.isBoss ? '#ff5b5b' : '#7ee08a';
    ctx.fillRect(x - bw / 2, by, bw * (e.hp / d.hp), bh);
  }
}

function draw() {
  ctx.clearRect(0, 0, cv.width, cv.height);

  // Map: sprite PPU 100 -> kích thước thế giới = pixel / 100
  const mw = IMG.Map.width / MAP_PPU, mh = IMG.Map.height / MAP_PPU;
  drawSprite(IMG.Map, 0, 0, mw, mh);

  // Đế tháp
  const slotW = IMG.Tower_Slot.width / 1000, slotH = IMG.Tower_Slot.height / 1000;
  for (const s of Game.slots) {
    const isHover = Game.hoverSlot === s && Game.selected && !s.tower;
    ctx.save();
    if (isHover) {
      const afford = Game.coins >= Game.selected.cost;
      ctx.filter = afford ? 'brightness(1.35) hue-rotate(60deg)' : 'brightness(1.2) hue-rotate(-40deg)';
    }
    drawSprite(IMG.Tower_Slot, s.x, s.y, slotW, slotH);
    ctx.restore();

    if (isHover) {
      ctx.beginPath();
      ctx.arc(toScreenX(s.x), toScreenY(s.y), Game.selected.range * PXU, 0, Math.PI * 2);
      ctx.strokeStyle = '#7ee08acc'; ctx.lineWidth = 2; ctx.stroke();
      ctx.fillStyle = '#7ee08a18'; ctx.fill();
    }
  }

  // Tháp đã xây
  for (const s of Game.slots) {
    if (!s.tower) continue;
    const img = IMG[s.tower.def.img];
    const ppu = s.tower.def.ppu;
    drawSprite(img, s.x, s.y - 0.35, img.width / ppu, img.height / ppu, { bottom: true });
  }

  for (const e of allEnemies()) drawEnemy(e);

  for (const b of Game.bulletPool.activeItems) {
    const img = IMG[b.def.proj];
    const w = img.width / b.def.projPpu, h = img.height / b.def.projPpu;
    drawSprite(img, b.x, b.y, w, h, b.def.attack === 'splash' ? {} : { angle: b.angle || 0 });
  }
}

// ── Vòng lặp ───────────────────────────────────────────────────────────────

let lastTime = 0;

function loop(now) {
  const dt = Math.min(0.05, (now - lastTime) / 1000) || 0;
  lastTime = now;

  if (Game.state === 'playing') {
    for (const e of allEnemies()) updateEnemy(e, dt);
    for (const s of Game.slots) updateTower(s, dt);
    for (const b of Game.bulletPool.activeItems) updateBullet(b, dt);
  }

  draw();
  requestAnimationFrame(loop);
}

// ── Giao diện ──────────────────────────────────────────────────────────────

function refreshHud() {
  document.getElementById('lives').textContent = Game.lives;
  document.getElementById('coins').textContent = Game.coins;
  document.getElementById('wave').textContent =
    Game.wave <= 0 ? 'Chuẩn bị…' : `Đợt ${Game.wave}/${WAVES.length}`;
  renderShop();
}

function renderShop() {
  const shop = document.getElementById('shop');
  shop.innerHTML = '';

  for (const def of TOWER_TYPES) {
    const card = document.createElement('div');
    card.className = 'card'
      + (Game.selected === def ? ' sel' : '')
      + (Game.coins < def.cost ? ' poor' : '');
    card.innerHTML =
      `<img class="tw" src="assets/${def.img}.png" alt="">` +
      `<div class="nm">${def.name}</div>` +
      `<div class="cost">${def.cost} ⛃</div>` +
      `<div class="info">${def.info}<br>ST ${def.dmg} · tầm ${def.range}</div>`;
    card.onclick = () => {
      Audio2.ensure();
      Game.selected = Game.selected === def ? null : def;
      renderShop();
    };
    shop.appendChild(card);
  }
}

function setOverlay(id, on) { document.getElementById(id).classList.toggle('on', on); }

function pause()  { if (Game.state === 'playing') { Game.state = 'paused'; setOverlay('pauseOv', true); } }
function resume() { if (Game.state === 'paused')  { Game.state = 'playing'; setOverlay('pauseOv', false); setOverlay('setOv', false); } }

function bindUi() {
  document.getElementById('pauseBtn').onclick = () => { Audio2.ensure(); pause(); };

  document.body.addEventListener('click', ev => {
    const act = ev.target.dataset && ev.target.dataset.act;
    if (!act) return;
    if (act === 'resume') resume();
    if (act === 'restart') location.reload();
    if (act === 'settings') setOverlay('setOv', true);
    if (act === 'closeSettings') setOverlay('setOv', false);
  });

  const bind = (slider, label, fn) => {
    const s = document.getElementById(slider), l = document.getElementById(label);
    s.oninput = () => { l.textContent = s.value + '%'; fn(s.value / 100); };
  };
  bind('volMusic', 'volMusicV', v => Audio2.setMusic(v));
  bind('volSfx', 'volSfxV', v => Audio2.setSfx(v));

  addEventListener('keydown', ev => {
    if (ev.key !== 'Escape') return;
    if (document.getElementById('setOv').classList.contains('on')) { setOverlay('setOv', false); return; }
    Game.state === 'playing' ? pause() : resume();
  });

  cv.addEventListener('mousemove', ev => {
    const r = cv.getBoundingClientRect();
    Game.mouse.wx = toWorldX(ev.clientX - r.left);
    Game.mouse.wy = toWorldY(ev.clientY - r.top);

    Game.hoverSlot = null;
    for (const s of Game.slots) {
      if (Math.hypot(Game.mouse.wx - s.x, Game.mouse.wy - s.y) <= SLOT_CLICK_RADIUS) {
        Game.hoverSlot = s; break;
      }
    }
    cv.style.cursor = Game.hoverSlot && Game.selected ? 'pointer' : 'crosshair';
  });

  cv.addEventListener('click', () => {
    Audio2.ensure(); Audio2.startMusic();
    if (Game.state !== 'playing' || !Game.selected || !Game.hoverSlot) return;
    buildTower(Game.hoverSlot, Game.selected);
  });

  cv.addEventListener('contextmenu', ev => {
    ev.preventDefault();
    Game.selected = null;
    renderShop();
  });

  addEventListener('resize', resize);
}

// ── Khởi động ──────────────────────────────────────────────────────────────

loadAll().then(data => {
  scene = data;
  Game.path = data.waypoints;
  Game.slots = data.slots.map(([x, y]) => ({ x, y, tower: null }));

  for (const k in ENEMY_TYPES) Game.enemyPools[k] = new Pool(() => makeEnemy(k));
  Game.bulletPool = new Pool(() => ({ active: false, x: 0, y: 0, target: null, def: null, life: 0 }));

  cv = document.getElementById('cv');
  ctx = cv.getContext('2d');
  resize();
  bindUi();
  refreshHud();

  requestAnimationFrame(loop);
  runWaves();
}).catch(err => {
  document.body.innerHTML =
    `<div style="padding:40px;font-size:18px;line-height:1.6">
       <b>Không tải được tài nguyên.</b><br>${err.message}<br><br>
       Mở bằng cách chạy máy chủ tĩnh trong thư mục WebPreview:<br>
       <code style="background:#222;padding:4px 8px;border-radius:4px">python3 -m http.server 8080</code>
       rồi vào <code>http://localhost:8080</code>
     </div>`;
});
