// Kitchen scene artwork.
//
// Flat vector, no gradients on props, rounded everything. Three rules the
// mechanics impose on the drawing, not the other way round:
//
//   1. Cuttable objects need one clean silhouette. A shape with holes or thin
//      tendrils produces cut pieces that look like mistakes.
//   2. Paintable objects need a large flat region with no detail in it. Detail
//      belongs around the paintable area, never inside it.
//   3. Nothing carries baked-in text. A drawn word is a localised asset, and
//      that multiplies the art budget by the number of languages.
//
// Sprite dimensions match placeholderSize in the manifest, so the two stay in
// step and swapping a sprite in changes nothing about layout.

import { palette as c, svg, outline } from './palette.mjs';

const backdrop = () => svg(1920, 1080, `
  <defs>
    <linearGradient id="wall" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="${c.wallTop}"/>
      <stop offset="1" stop-color="${c.wallBottom}"/>
    </linearGradient>
  </defs>
  <rect width="1920" height="1080" fill="url(#wall)"/>
  <g opacity="0.35">
    ${[300, 700, 1100, 1500].map((x) => `<rect x="${x}" y="0" width="4" height="880" fill="${c.skirting}"/>`).join('')}
  </g>
  <rect x="0" y="866" width="1920" height="18" fill="${c.skirting}"/>
  <rect x="0" y="884" width="1920" height="196" fill="${c.floor}"/>
  <g opacity="0.5">
    ${[0, 240, 480, 720, 960, 1200, 1440, 1680].map((x) => `<rect x="${x}" y="884" width="6" height="196" fill="${c.floorDark}"/>`).join('')}
  </g>
`);

const counter = () => svg(640, 190, `
  <rect x="0" y="0" width="640" height="40" rx="14" fill="${c.worktop}"/>
  <rect x="0" y="30" width="640" height="12" fill="${c.woodLight}" opacity="0.5"/>
  <rect x="10" y="40" width="620" height="150" rx="12" fill="${c.woodMid}"/>
  <rect x="28" y="58" width="270" height="116" rx="10" fill="${c.woodLight}"/>
  <rect x="342" y="58" width="270" height="116" rx="10" fill="${c.woodLight}"/>
  <rect x="150" y="70" width="26" height="8" rx="4" fill="${c.metalDark}"/>
  <rect x="464" y="70" width="26" height="8" rx="4" fill="${c.metalDark}"/>
  <rect x="10" y="40" width="620" height="150" rx="12" ${outline()}/>
`);

const oven = () => svg(220, 300, `
  <rect x="6" y="30" width="208" height="264" rx="20" fill="${c.slate}"/>
  <rect x="6" y="0" width="208" height="44" rx="16" fill="${c.slateDark}"/>
  <circle cx="58" cy="22" r="10" fill="${c.metalLight}"/>
  <circle cx="110" cy="22" r="10" fill="${c.metalLight}"/>
  <circle cx="162" cy="22" r="10" fill="${c.metalLight}"/>
  <rect x="26" y="86" width="168" height="150" rx="16" fill="${c.slateDark}"/>
  <rect x="40" y="100" width="140" height="122" rx="12" fill="${c.glassDeep}"/>
  <rect x="52" y="112" width="116" height="98" rx="8" fill="${c.glass}" opacity="0.65"/>
  <rect x="34" y="60" width="152" height="14" rx="7" fill="${c.metal}"/>
  <rect x="6" y="30" width="208" height="264" rx="20" ${outline()}/>
`);

const shelf = () => svg(600, 50, `
  <rect x="0" y="0" width="600" height="30" rx="10" fill="${c.woodLight}"/>
  <rect x="0" y="26" width="600" height="20" rx="8" fill="${c.woodDark}"/>
  <rect x="0" y="0" width="600" height="46" rx="10" ${outline(3)}/>
`);

const windowSprite = () => svg(320, 240, `
  <rect x="0" y="0" width="320" height="224" rx="16" fill="${c.woodMid}"/>
  <rect x="16" y="16" width="288" height="176" rx="8" fill="${c.sky}"/>
  <circle cx="242" cy="66" r="30" fill="${c.cream}" opacity="0.75"/>
  <circle cx="214" cy="76" r="22" fill="${c.cream}" opacity="0.75"/>
  <rect x="150" y="16" width="14" height="176" fill="${c.woodMid}"/>
  <rect x="16" y="96" width="288" height="14" fill="${c.woodMid}"/>
  <rect x="0" y="206" width="320" height="26" rx="8" fill="${c.woodLight}"/>
  <rect x="0" y="0" width="320" height="232" rx="16" ${outline()}/>
`);

const tray = () => svg(430, 45, `
  <rect x="0" y="8" width="430" height="30" rx="10" fill="${c.metal}"/>
  <rect x="12" y="0" width="406" height="26" rx="8" fill="${c.metalLight}"/>
  <rect x="0" y="8" width="430" height="34" rx="10" ${outline(3)}/>
`);

// Two dough blobs, deliberately different silhouettes: the cut mechanic is much
// more satisfying when the pieces are not identical every time.
// Flour dust rather than dimples: two dots and a curved line read as a face, and a
// face on the object you are about to cut in half is the wrong idea entirely.
const flourDust = (points) => points
  .map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${c.doughShade}" opacity="0.75"/>`)
  .join('');

const doughA = () => svg(150, 110, `
  <path d="M18 62 C10 30 38 8 74 10 C112 12 140 30 138 60 C136 92 108 104 74 102 C42 100 24 88 18 62 Z" fill="${c.dough}"/>
  ${flourDust([[42, 38, 3], [66, 30, 2.5], [98, 44, 3], [58, 76, 2.5], [110, 70, 2], [34, 62, 2]])}
  <path d="M18 62 C10 30 38 8 74 10 C112 12 140 30 138 60 C136 92 108 104 74 102 C42 100 24 88 18 62 Z" ${outline(3)}/>
`);

const doughB = () => svg(150, 110, `
  <path d="M14 56 C16 24 46 10 80 12 C118 14 138 36 136 66 C134 94 100 106 66 100 C34 94 12 84 14 56 Z" fill="${c.dough}"/>
  ${flourDust([[46, 34, 2.5], [78, 28, 3], [108, 48, 2.5], [50, 74, 3], [92, 80, 2], [120, 66, 2.5]])}
  <path d="M14 56 C16 24 46 10 80 12 C118 14 138 36 136 66 C134 94 100 106 66 100 C34 94 12 84 14 56 Z" ${outline(3)}/>
`);

const biscuit = (shape) => svg(95, 95, `
  ${shape}
  <circle cx="38" cy="44" r="4" fill="${c.biscuitDark}"/>
  <circle cx="58" cy="54" r="4" fill="${c.biscuitDark}"/>
  <circle cx="50" cy="34" r="3.5" fill="${c.biscuitDark}"/>
`);

const star = `<path d="M47.5 6 L59 34 L89 37 L66 57 L73 87 L47.5 71 L22 87 L29 57 L6 37 L36 34 Z" fill="${c.biscuit}" stroke="${c.biscuitDark}" stroke-width="4" stroke-linejoin="round"/>`;
const heart = `<path d="M47.5 88 C10 62 8 36 22 24 C34 14 45 20 47.5 30 C50 20 61 14 73 24 C87 36 85 62 47.5 88 Z" fill="${c.biscuit}" stroke="${c.biscuitDark}" stroke-width="4" stroke-linejoin="round"/>`;
const moon = `<path d="M62 8 A42 42 0 1 0 62 87 A34 34 0 1 1 62 8 Z" fill="${c.biscuit}" stroke="${c.biscuitDark}" stroke-width="4" stroke-linejoin="round"/>`;

// The cake is the paintable object: the top is one flat region with nothing in it,
// so a fill reads as decorating rather than colouring over detail.
const cakePlain = () => svg(220, 200, `
  <ellipse cx="110" cy="186" rx="94" ry="12" fill="${c.line}" opacity="0.1"/>
  <path d="M18 62 L18 160 A92 22 0 0 0 202 160 L202 62 Z" fill="${c.biscuit}"/>
  <path d="M18 62 L18 96 C40 112 66 104 88 96 C112 88 140 104 164 100 C180 97 194 88 202 80 L202 62 Z" fill="${c.cream}"/>
  <ellipse cx="110" cy="62" rx="92" ry="24" fill="${c.cream}"/>
  <ellipse cx="110" cy="62" rx="78" ry="18" fill="${c.cream}" stroke="${c.pink}" stroke-width="3" stroke-opacity="0.5"/>
  <path d="M18 62 L18 160 A92 22 0 0 0 202 160 L202 62" ${outline(3)}/>
`);

const jar = (contents, lid) => svg(85, 120, `
  <rect x="10" y="24" width="65" height="90" rx="14" fill="${c.glass}"/>
  <rect x="16" y="52" width="53" height="58" rx="10" fill="${contents}"/>
  <rect x="10" y="24" width="65" height="90" rx="14" fill="none" stroke="${c.glassDeep}" stroke-width="4"/>
  <rect x="20" y="34" width="10" height="46" rx="5" fill="#FFFFFF" opacity="0.5"/>
  <rect x="4" y="6" width="77" height="26" rx="10" fill="${lid}"/>
  <rect x="4" y="6" width="77" height="26" rx="10" ${outline(3)}/>
`);

// An original character rather than a licensed one — the competitor analysis puts
// that as the lowest-friction of the three IP routes, and it keeps the brand ours.
const maker = () => svg(180, 180, `
  <ellipse cx="90" cy="172" rx="46" ry="7" fill="${c.line}" opacity="0.12"/>
  <rect x="52" y="96" width="76" height="72" rx="22" fill="${c.teal}"/>
  <path d="M66 96 L114 96 L106 152 L74 152 Z" fill="${c.cream}"/>
  <rect x="30" y="104" width="26" height="54" rx="13" fill="${c.teal}"/>
  <rect x="124" y="104" width="26" height="54" rx="13" fill="${c.teal}"/>
  <circle cx="38" cy="158" r="13" fill="${c.dough}"/>
  <circle cx="142" cy="158" r="13" fill="${c.dough}"/>
  <circle cx="90" cy="62" r="42" fill="${c.dough}"/>
  <path d="M48 54 C50 24 76 14 90 14 C104 14 130 24 132 54 C120 40 100 36 90 36 C80 36 60 40 48 54 Z" fill="${c.woodDark}"/>
  <circle cx="74" cy="66" r="6" fill="${c.line}"/>
  <circle cx="106" cy="66" r="6" fill="${c.line}"/>
  <path d="M78 82 C84 90 96 90 102 82" stroke="${c.line}" stroke-width="5" stroke-linecap="round" fill="none"/>
  <circle cx="58" cy="78" r="7" fill="${c.pink}" opacity="0.55"/>
  <circle cx="122" cy="78" r="7" fill="${c.pink}" opacity="0.55"/>
`);

/// id -> SVG. Ids match the sprite names declared in the scene manifest.
export const sprites = {
  backdrop: backdrop(),
  counter: counter(),
  oven: oven(),
  shelf: shelf(),
  window: windowSprite(),
  tray: tray(),
  dough_a: doughA(),
  dough_b: doughB(),
  cookie_star: biscuit(star),
  cookie_heart: biscuit(heart),
  cookie_moon: biscuit(moon),
  cake_plain: cakePlain(),
  jar_flour: jar('#F7F1E6', c.red),
  jar_sugar: jar('#FBF7F0', c.teal),
  jar_salt: jar('#EFF2F4', c.yellow),
  jar_cocoa: jar('#8C5A3C', c.green),
};

export const characters = {
  maker: maker(),
};
