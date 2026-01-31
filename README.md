# Sticksmith

Sticksmith is a web-based drum practice tool. It listens to MIDI drum input and matches your playing against predefined patterns (rudiments and drumkit grooves), providing real-time feedback on timing, dynamics, and sticking accuracy.

## Features

- **Pattern matching** for rudiments and drumkit beats
- **Visual notation** with a moving cursor and error overlays
- **Performance metrics** for timing, dynamics, and sticking
- **Sticky locking** that follows the pattern once you’re consistently on it
- **MIDI monitor** page for debugging and device verification

## Usage

Open `rudiments.html` in a browser with Web MIDI support (Chrome recommended). Connect a MIDI drum pad or electronic kit, configure your stick mapping (left/right notes), and start playing.

For device debugging or mapping discovery, open `monitor.html`.

---

## Architecture

### Pattern Types

1. **Rudiments** — single-line patterns for practice pad work
   - Focus on sticking (R/L)
   - Dynamics (accents, ghost notes)
   - Compound strokes (doubles, flams, drags)

2. **Drumkit Patterns** — multi-instrument grooves
   - Separate voice tracks per instrument (hihat, snare, kick, toms, cymbals)
   - 5-line staff notation
   - Per-instrument matching and scoring
   - Multi-instrument progress tracking (primary instrument drives progress heuristics)

### Pattern Definition DSL

Patterns are defined in `PATTERN_DEFS` using a JSON-like DSL:

```javascript
{
  "pattern-id": {
    name: "Human Readable Name",
    type: 'rudiment' | 'drumkit',  // default: 'rudiment'
    time: 4,                        // beats per measure (default: 4)
    beamGroup: 4,                   // notes per beam group (default: 4)
    
    // For rudiments: linear note array
    notes: [
      { type: 'single', hand: 'R', dur: 16, vel: 'accent' },
      { type: 'double', hand: 'L', dur: 16 },
      { type: 'flam', hand: 'R', dur: 8 },
      { type: 'drag', hand: 'L', dur: 8 },
      { type: 'rest', dur: 4 },
    ],
    
    // For drumkit: voice-based tracks
    voices: {
      hihat: [
        { type: 'single', dur: 8 },
        { type: 'rest', dur: 8 },
      ],
      snare: [
        { type: 'rest', dur: 4 },
        { type: 'single', dur: 4 },
      ],
      kick: [
        { type: 'single', dur: 4 },
        { type: 'rest', dur: 4 },
      ]
    }
  }
}
```

### Note Types

| Type | Description | Hits |
|------|-------------|------|
| `single` | Single stroke | 1 |
| `double` | Double stroke (diddle) | 2 |
| `flam` | Grace note + main note | 2 |
| `drag` | Two grace notes + main | 3 |
| `rest` | Silence | 0 |

### Duration Values

| Value | Name | Slots (16th = 1) |
|-------|------|------------------|
| 1 | Whole | 16 |
| 2 | Half | 8 |
| 4 | Quarter | 4 |
| 8 | Eighth | 2 |
| 12 | Triplet Eighth | 4/3 |
| 16 | Sixteenth | 1 |

### Velocity / Dynamics

- `'accent'` or `ACCENT` → MIDI velocity 127
- `'normal'` or `NORMAL` → MIDI velocity 64
- `'ghost'` or `GHOST` → MIDI velocity 32

---

## MIDI Input

### Device Handling

- Uses Web MIDI API (`navigator.requestMIDIAccess`)
- Listens to all connected MIDI inputs
- Handles device connect/disconnect events
- Supports stick mapping via note lists and a MIDI-learn flow

### Drum Mapping (Custom Kit)

MIDI note numbers are mapped to instrument names in `rudiments.html`:

```javascript
MIDI_DRUM_MAP = {
  36: 'kick',   // Kick
  38: 'snare',  // Snare (wires on)
  19: 'snare',  // Snare (wires off)
  46: 'hihat',  // Hi-hat bow (open)
  26: 'hihat',  // Hi-hat edge (open)
  42: 'hihat',  // Hi-hat bow (closed)
  22: 'hihat',  // Hi-hat edge (closed)
  // toms/cymbals as defined in MIDI_DRUM_MAP
}
```

### Match Buffer

Incoming notes are stored in a rolling buffer:

```javascript
{ time: ms, velocity: 0-127, sticking: 'R'|'L'|'', inst: 'snare'|'kick'|... }
```

Buffer management:
- Pruned by age (`SCORE_DECAY_MS`)
- Pruned by count (`MAX_MATCH_BEATS` for rudiments, `MAX_MATCH_EVENTS_DRUMKIT` for drumkit)
- Reset on large gaps (`MATCH_RESET_GAP_MS`)
- Window length also respects the extended pattern length for the current selection

---

## Pattern Matching Algorithm

### Overview

The matching algorithm finds the best time-scaling to align played notes with the pattern. It explores different scale/translate combinations and scores each one.

### For Rudiments (Single Instrument)

1. Pick pairs of pattern notes (p0, p1) and match notes (m0, m1)
2. Calculate time scale: `scale = (p1.time - p0.time) / (m1.time - m0.time)`
3. Calculate translation: `translate = p0.time - m0.time * scale`
4. Apply scale/translate to all match notes
5. Score the alignment
6. Keep best candidates

### For Drumkit (Multi-Instrument)

1. **Identify matchable instruments** — instruments present in both pattern and match
2. **Try each instrument as "primary"**:
   - Use pairs from that instrument to determine time scale
   - Apply scale to ALL instruments
   - Score ALL instruments together
   - Track progress using the primary instrument to avoid hi-hat dominating progress
3. **Pick best combination** — lowest overall score wins

### Scoring

Per-instrument scoring (`scoreSingleInstrument`):
- Timing error: squared difference between played and pattern times
- Velocity error: squared difference in velocities
- Reuse penalty: hitting the same pattern note twice
- Gap penalty: missed notes within the matched range

Combined scoring (`scorePerInstrument`):
- Scores each instrument independently
- Uses per-instrument match windows (avoids penalizing snare/kick for dense hi-hat)
- Weights by number of notes per instrument
- Combines into total score

Rudiment scoring (`scoreRudiment`):
- Same timing/velocity scoring
- Plus sticking mismatch penalties
- Time-weighted (recent notes matter more)

### Pattern Locking

- Auto-locks when a pattern is consistently best (`STICKY_*` thresholds)
- Unlocks after a pause or a sustained bad-score window
- Locked mode only searches the current pattern

---

## Notation Rendering

### Shared Note Dimensions

```javascript
NOTE_CONFIG = {
  headRx: 8,        // ellipse x-radius
  headRy: 5.5,      // ellipse y-radius
  xHeadSize: 10,    // X notehead size (cymbals)
  angleRad: -25°,   // rotation angle
  stemWidth: 1.5
}

STEM_OFFSET_X = stemAnchor(...).offsetX * 0.85  // stem attachment point
```

### Shared Functions

- `drawNotehead(svg, x, y, isXHead, color, isMiss, container)` — draws oval or X notehead
- `drawStem(svg, x, bottomY, topY, color, container)` — draws vertical stem
- `stemAnchor(rx, ry, angle)` — calculates stem attachment point on rotated ellipse

### Rudiment Notation (`renderNotationSVG`)

- Single horizontal staff line
- Notes above/below line based on sticking (R below, L above)
- Sticking labels (R/L)
- Beamed groups
- Grace notes for flams/drags
- Accent marks, ghost parentheses

### Drumkit Notation (`renderDrumkitSVG`)

- 5-line staff with drum clef
- Instrument positions (`DRUM_STAFF`):
  ```
  crash:  line -1    (above staff)
  ride:   line -0.5  (above top line)
  hihat:  line -0.5  (above top line)
  tom1:   line 0.5   (top space)
  tom2:   line 1.5   (second space)
  snare:  line 1.5   (second space)
  tom3:   line 2.5   (third space)
  kick:   line 3.5   (bottom space)
  ```
- X noteheads for cymbals
- Oval noteheads for drums
- Single stem connecting all notes at the same time slot
- Stem X position: same for all notes (`STEM_OFFSET_X` from note center)
- Special case: hi-hat only → stem starts at top of X

### Error Note Rendering (`renderTimingErrors`)

- Uses the same shared functions as main notation
- Red color for errors
- Hollow noteheads for misses
- Dashed X for missed cymbals
- Short stems (not connected to beam)

---

## Visual Feedback

### Debug Canvas (`renderPatternMatch`)

Shows pattern vs match alignment:

**Single Instrument (Rudiments):**
- Top band: pattern notes (gray bars)
- Bottom band: match notes (blue bars)
- Sticking labels
- Height = velocity

**Multi-Instrument (Drumkit):**
- One row per instrument
- Each row: pattern (top half) + match (bottom half)
- Instrument label on left
- Different color shades per instrument

**Metrics Area:**
- Dynamics bar (velocity accuracy)
- Timing bar (timing accuracy)
- Sticking bar (hand accuracy)
- Feel indicator (rushing/dragging/on-time + BPM)

### Notation Cursor

- Highlights current pattern position
- Red marker on the notation
- Moves forward as pattern is matched

### Timing Error Markers

- Red notes showing where errors occurred
- Position reflects timing offset
- Instrument-aware for drumkit patterns

---

## File Structure

```
sticksmith/
├── rudiments.html      # Main application (all-in-one HTML/CSS/JS)
├── monitor.html        # MIDI monitor/debugging
├── index.html          # Landing page
├── snare.html          # Snare-focused practice
├── notation/           # Standalone notation viewer
│   ├── index.html
│   ├── app.js
│   └── style.css
├── strudel/            # Strudel integration experiments
├── trigger/            # Arduino trigger code
├── MIDI/               # F# MIDI generation tools
└── README.md           # This document
```

---

## Key Constants

```javascript
// Matching
SCORE_DECAY_MS = 8000            // Match buffer time window
MAX_MATCH_BEATS = 16             // Max notes in buffer (rudiments)
MAX_MATCH_EVENTS_DRUMKIT = 48    // Max notes in buffer (drumkit)
MATCH_RESET_GAP_MS = 3000        // Gap that resets buffer
NOTE_AGE_CUTOFF_MS = 8000        // Ignore old notes in scoring

// Scoring
DISQUALIFIED_SCORE = 1e12        // Score for invalid matches
STICKING_MISMATCH_BASE = 50000   // Penalty for wrong hand
DYNAMICS_WEIGHT = 3              // Velocity importance

// Locking
STICKY_MIN_NOTES = 6             // Notes needed to lock
STICKY_SCORE_THRESHOLD = 15      // Max score to lock
STICKY_PAUSE_MS = 2000           // Pause that unlocks
STICKY_BAD_SCORE_THRESHOLD = 50  // Score that unlocks

// Progress
LOCKED_JITTER_TOLERANCE = 2
LOCKED_JUMP_PENALTY_PER_NOTE = 100
PROGRESS_PENALTY = 20
```

---

## To Do

### Gap Click Exercise
A metronome training mode where the click periodically goes silent for a set number of bars. You continue playing to keep time internally, then the click returns and you're scored on how well you stayed in sync.

### Timing Graph
A visual display with rows for each instrument showing timing deviation over time. A center band represents "in the pocket" — deviations to the right indicate rushing, to the left indicate dragging. Helps identify timing tendencies on specific instruments.

---

## Future Considerations

- [ ] Metronome with configurable subdivisions
- [ ] Recording and playback of practice sessions
- [ ] Progress tracking over time
- [ ] More drumkit patterns (jazz, Latin, etc.)
- [ ] Visual tempo guide / scrolling notation
- [ ] Audio playback of patterns
- [ ] Mobile/touch support
