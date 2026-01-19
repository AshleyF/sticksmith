const statusPill = document.getElementById("status");
const notationEl = document.getElementById("notation");

const rudiments = {
  paradiddle: { name: "Single paradiddle", sticking: "LRLLRLRRLRLLRLRR", accentEvery: 8 },
};

function rotateSticking(sticking, desiredStart) {
  const hands = sticking.trim().toUpperCase();
  if (!hands.length) return "R";
  const offset = hands[0] === desiredStart ? 0 : 1;
  const rotated = hands.slice(offset) + hands.slice(0, offset);
  return rotated;
}

function buildHits(rudimentKey, subdivision, bars, startHand) {
  const rudiment = rudiments[rudimentKey] || rudiments.paradiddle;
  const dur = subdivision === "8" ? "8" : "16";
  const beatValue = dur === "8" ? 0.5 : 0.25;
  const totalBeats = 4 * Math.max(1, Math.min(2, bars));
  const pattern = rudiment.sticking.trim().toUpperCase();
  const hits = [];

  for (let i = 0; i < totalBeats / beatValue; ) {
    const hand = pattern[i % pattern.length];
    const accent = rudiment.accentEvery ? i % rudiment.accentEvery === 0 : false;

    hits.push({ hand, duration: dur, accent });
    i += 1;
  }
  return hits;
}

function keyForHand(hand) {
  // These map to vertical positions; actual note names are not used now but kept for clarity.
  return hand === "R" ? "c/5" : "a/4";
}

function renderStaff() {
  const rudimentKey = "paradiddle";
  const subdivision = "16";
  const bars = 1;
  const startHand = "R";

  statusPill.textContent = "Rendering";
  clearChildren(notationEl);

  const clampedBars = Math.max(1, Math.min(2, bars));
  const totalBeats = 4 * clampedBars;
  const hits = buildHits(rudimentKey, subdivision, clampedBars, startHand);

  const svg = createSvg(780, 260);
  notationEl.appendChild(svg);

  const marginL = 28;
  const marginR = 28;
  const staffWidth = 780 - marginL - marginR;
  const centerY = 130;
  const lineThickness = 2.4;
  const lineHalf = lineThickness * 0.5;
  const line = lineElement(marginL, centerY, marginL + staffWidth, centerY, lineThickness, "#ffffff");
  svg.appendChild(line);

  // Draw time signature text
  svg.appendChild(textElement(marginL - 4, centerY - 20, `${totalBeats}/4`, "12px", "end", "rgba(255,255,255,0.8)"));

  const beatValue = subdivision === "8" ? 0.5 : 0.25;
  const spacing = staffWidth / totalBeats;
  const beamY = centerY - 44; // lower to cover stem joins
  const minStem = 28;
  const beamThickness = 5;
  const headRx = 9;
  const headRy = 6.5;
  const headFill = "#ffffff";
  const strokeTone = "#ffffff";
  const angleRad = -25 * Math.PI / 180;
  const { offsetX: stemOffsetX, offsetY: stemOffsetY } = stemAnchorForHead(headRx, headRy, angleRad);

  let beatCursor = 0;
  const placed = [];

  hits.forEach(hit => {
    const x = marginL + beatCursor * spacing + spacing * beatValue * 0.5;
    const y = hit.hand === "R"
      ? centerY - headRy - lineHalf
      : centerY + headRy + lineHalf;
    const dir = 1; // all stems up
    const stemX = x + stemOffsetX * 0.9;
    const stemStartY = y + stemOffsetY;
    const head = notehead(x, y, headFill, headRx, headRy, angleRad);
    const stemLen = Math.max(minStem, stemStartY - beamY + 1);
    const stem = stemElement(stemX, stemStartY, stemLen * dir, strokeTone);
    svg.appendChild(head);
    svg.appendChild(stem);

    if (hit.accent) {
      svg.appendChild(accentMark(x, y - stemLen - 12, strokeTone));
    }

    placed.push({ x, stemX, dir, duration: hit.duration, stemEndY: beamY });
    beatCursor += beatValue;
  });

  const beamGroups = chunkBeams(placed, 4);
  beamGroups.forEach(group => {
    const yRef = beamY;
    const xStart = group.notes[0].stemX;
    const xEnd = group.notes[group.notes.length - 1].stemX;
    const rect1 = beamRect(xStart, xEnd, yRef, 1, beamThickness, strokeTone);
    const rect2 = beamRect(xStart, xEnd, yRef + beamThickness + 4, 1, beamThickness, strokeTone);
    svg.appendChild(rect1);
    svg.appendChild(rect2);
  });

  statusPill.textContent = "Ready";
}

function clearChildren(el) {
  while (el.firstChild) el.removeChild(el.firstChild);
}

renderStaff();

// --- SVG helpers ---
function createSvg(width, height) {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("width", width);
  svg.setAttribute("height", height);
  svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
  svg.setAttribute("fill", "none");
  return svg;
}

function lineElement(x1, y1, x2, y2, strokeWidth, color) {
  const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
  line.setAttribute("x1", x1);
  line.setAttribute("y1", y1);
  line.setAttribute("x2", x2);
  line.setAttribute("y2", y2);
  line.setAttribute("stroke", color);
  line.setAttribute("stroke-width", strokeWidth);
  line.setAttribute("stroke-linecap", "round");
  return line;
}

function textElement(x, y, text, size, anchor, color) {
  const t = document.createElementNS("http://www.w3.org/2000/svg", "text");
  t.setAttribute("x", x);
  t.setAttribute("y", y);
  t.setAttribute("fill", color);
  t.setAttribute("font-size", size);
  t.setAttribute("font-family", "Space Grotesk, system-ui, sans-serif");
  t.setAttribute("text-anchor", anchor || "start");
  t.textContent = text;
  return t;
}

function notehead(x, y, fill, rx = 9, ry = 6.5, angleRad = -25 * Math.PI / 180) {
  const head = document.createElementNS("http://www.w3.org/2000/svg", "ellipse");
  head.setAttribute("cx", x);
  head.setAttribute("cy", y);
  head.setAttribute("rx", rx);
  head.setAttribute("ry", ry);
  head.setAttribute("fill", fill);
  head.setAttribute("transform", `rotate(${angleRad * 180 / Math.PI} ${x} ${y})`);
  return head;
}

function stemElement(x, yStart, length, color) {
  const stem = document.createElementNS("http://www.w3.org/2000/svg", "line");
  stem.setAttribute("x1", x);
  stem.setAttribute("y1", yStart);
  stem.setAttribute("x2", x);
  stem.setAttribute("y2", yStart - length);
  stem.setAttribute("stroke", color);
  stem.setAttribute("stroke-width", 2.2);
  stem.setAttribute("stroke-linecap", "round");
  return stem;
}

function accentMark(x, y, color) {
  const accent = document.createElementNS("http://www.w3.org/2000/svg", "path");
  const w = 9;
  const h = 5;
  // Draw a classic ">" accent above the note
  accent.setAttribute("d", `M ${x - w} ${y - h} L ${x + w} ${y} L ${x - w} ${y + h}`);
  accent.setAttribute("fill", "none");
  accent.setAttribute("stroke", color || "#ffffff");
  accent.setAttribute("stroke-width", 2.2);
  accent.setAttribute("stroke-linecap", "round");
  accent.setAttribute("stroke-linejoin", "round");
  return accent;
}

function beamRect(xStart, xEnd, yRef, dir, thickness, color) {
  const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
  const x = Math.min(xStart, xEnd) - 1.2;
  const width = Math.abs(xEnd - xStart) + 2.4;
  const y = dir === 1 ? yRef - thickness : yRef;
  rect.setAttribute("x", x);
  rect.setAttribute("y", y);
  rect.setAttribute("width", width);
  rect.setAttribute("height", thickness);
  rect.setAttribute("fill", color);
  rect.setAttribute("rx", "2");
  rect.setAttribute("ry", "2");
  return rect;
}

function chunkBeams(notes, size) {
  const groups = [];
  for (let i = 0; i < notes.length; i += size) {
    const slice = notes.slice(i, i + size);
    if (slice.length > 1) groups.push({ notes: slice });
  }
  return groups;
}

function stemAnchorForHead(rx, ry, angleRad) {
  // Point on rotated ellipse where tangent is vertical; returns offsets from center.
  const sinA = Math.sin(angleRad);
  const cosA = Math.cos(angleRad);
  const t = Math.atan(-(ry * sinA) / (rx * cosA));
  const x = rx * Math.cos(t);
  const y = ry * Math.sin(t);
  const X = x * cosA - y * sinA;
  const Y = x * sinA + y * cosA;
  return { offsetX: Math.abs(X), offsetY: Y };
}
