// The scene palette, in one place.
//
// Warm and desaturated on purpose. The category leaders sit in this register and
// it is not decoration: saturated primaries read as "toy shop", they compete with
// each other for attention, and a two-year-old already has more to look at than
// they can process. Muted colours let the object a child is touching be the
// brightest thing on screen.
//
// Every sprite pulls from here. A colour that appears in only one sprite belongs
// in that sprite; a colour that appears twice belongs in this file.

export const palette = {
  // Room
  wallTop: '#F3E7D5',
  wallBottom: '#EADCC6',
  floor: '#C99B6A',
  floorDark: '#B4854F',
  skirting: '#E4D2B6',

  // Wood
  woodDark: '#A9703F',
  woodMid: '#C68B54',
  woodLight: '#DCA772',
  worktop: '#F5EDE1',

  // Metal
  metal: '#C3C9D0',
  metalDark: '#9AA2AC',
  metalLight: '#DDE2E7',

  // Accents
  slate: '#6F7F90',
  slateDark: '#5A6878',
  red: '#DF6C5B',
  teal: '#5FB6AE',
  yellow: '#F0C051',
  pink: '#E9A0A8',
  green: '#8FBF6A',

  // Food
  dough: '#F0DCBB',
  doughShade: '#E2C99F',
  biscuit: '#D9A066',
  biscuitDark: '#C08B54',
  cream: '#FBEFE6',

  // Glass and line work
  glass: '#CFE4E8',
  glassDeep: '#AECDD4',
  sky: '#BEDDE8',
  line: '#54443A',
};

/// Soft dark outline used sparingly. Full black would be harsh at this age.
export const outline = (width = 4) => `fill="none" stroke="${palette.line}" stroke-opacity="0.28" stroke-width="${width}" stroke-linejoin="round"`;

export const svg = (width, height, body) =>
  `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">${body}</svg>`;
