# StickSmith Architecture

## Overview

StickSmith is a web-based drum practice application that provides real-time feedback on timing, dynamics, and technique. It accepts MIDI input from electronic drum kits or practice pads and matches played notes against predefined patterns (rudiments and full drumkit beats).

## Core Concepts

### Pattern Types

1. **Rudiments** - Single-line patterns played on a practice pad
   - Focus on sticking (R/L hand patterns)
   - Dynamics (accents, ghost notes)
   - Compound strokes (doubles, flams, drags)
   
2. **Drumkit Patterns** - Multi-instrument patterns
   - Separate voice tracks per instrument (hihat, snare, kick, toms, cymbals)
   - 5-line staff notation
   - Per-instrument matching and scoring

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

### Velocity/Dynamics

- `'accent'` or `ACCENT` → MIDI velocity 127
- `'normal'` or `NORMAL` → MIDI velocity 64
- `'ghost'` or `GHOST` → MIDI velocity 32

---

## MIDI Input

### Device Handling

- Uses Web MIDI API (`navigator.requestMIDIAccess`)
- Listens to all connected MIDI inputs
- Handles device connect/disconnect events
- Filters by configurable note numbers (default: snare notes)

### GM Drum Mapping

MIDI note numbers are mapped to instrument names:

```javascript
MIDI_DRUM_MAP = {
  35: 'kick',      // Acoustic Bass Drum
  36: 'kick',      // Bass Drum 1
  38: 'snare',     // Acoustic Snare
  40: 'snare',     // Electric Snare
  42: 'hihat',     // Closed Hi-Hat
  46: 'hihat',     // Open Hi-Hat
  49: 'crash',     // Crash Cymbal
  51: 'ride',      // Ride Cymbal
  // ... etc
}
```

### Match Buffer

Incoming notes are stored in a rolling buffer:
```javascript
{ time: ms, velocity: 0-127, sticking: 'R'|'L'|'', inst: 'snare'|'kick'|... }
```

Buffer management:
- Pruned by age (SCORE_DECAY_MS)
- Pruned by count (MAX_MATCH_BEATS)
- Reset on large gaps (MATCH_RESET_GAP_MS)

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

1. **Identify matchable instruments** - instruments present in both pattern and match
2. **Try each instrument as "primary"**:
   - Use pairs from that instrument to determine time scale
   - Apply scale to ALL instruments
   - Score ALL instruments together
3. **Pick best combination** - lowest overall score wins

This allows:
- Playing just snare → matches snare pattern notes
- Playing just kick → matches kick pattern notes
- Playing all instruments → finds scale that best aligns everything

### Scoring

Per-instrument scoring (`scoreSingleInstrument`):
- Timing error: squared difference between played and pattern times
- Velocity error: squared difference in velocities
- Reuse penalty: hitting same pattern note twice
- Gap penalty: missed notes within matched range

Combined scoring (`scorePerInstrument`):
- Scores each instrument independently
- Weights by number of notes per instrument
- Combines into total score

Rudiment scoring (`scoreRudiment`):
- Same timing/velocity scoring
- Plus sticking mismatch penalties
- Time-weighted (recent notes matter more)

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

- `drawNotehead(svg, x, y, isXHead, color, isMiss, container)` - draws oval or X notehead
- `drawStem(svg, x, bottomY, topY, color, container)` - draws vertical stem
- `stemAnchor(rx, ry, angle)` - calculates stem attachment point on rotated ellipse

### Rudiment Notation (`renderNotationSVG`)

- Single horizontal staff line
- Notes above/below line based on sticking (R below, L above)
- Sticking labels (R/L)
- Beamed groups
- Grace notes for flams/drags
- Accent marks, ghost parentheses

### Drumkit Notation (`renderDrumkitSVG`)

- 5-line staff with drum clef
- Instrument positions (DRUM_STAFF):
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
- Single stem connecting all notes at same time slot
- Stem X position: same for all notes (STEM_OFFSET_X from note center)
- Special case: hi-hat only → stem starts at top of X

### Error Note Rendering (`renderTimingErrors`)

- Uses same shared functions as main notation
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

## Pattern Locking

### Sticky Pattern Matching

Once a pattern is confidently identified, it "locks" to prevent jumping:

- **Lock conditions**: score ≤ threshold AND note count ≥ minimum
- **Unlock conditions**: 
  - Pause longer than STICKY_PAUSE_MS
  - Average score exceeds bad threshold over window

### Progress Tracking

When locked:
- Enforces forward progress through pattern
- Penalizes backward jumps
- Allows small jitter tolerance
- Handles wrap-around at pattern end

---

## File Structure

```
sticksmith/
├── rudiments.html      # Main application (all-in-one HTML/CSS/JS)
├── index.html          # Landing page
├── snare.html          # Snare-focused practice
├── notation/           # Standalone notation viewer
│   ├── index.html
│   ├── app.js
│   └── style.css
├── strudel/            # Strudel integration experiments
├── trigger/            # Arduino trigger code
├── MIDI/               # F# MIDI generation tools
└── architecture.md     # This document
```

---

## Key Constants

```javascript
// Matching
SCORE_DECAY_MS = 4000        // Match buffer time window
MAX_MATCH_BEATS = 32         // Max notes in buffer
MATCH_RESET_GAP_MS = 2000    // Gap that resets buffer
NOTE_AGE_CUTOFF_MS = 3000    // Ignore old notes in scoring

// Scoring
DISQUALIFIED_SCORE = 1e12    // Score for invalid matches
STICKING_MISMATCH_BASE = 50  // Penalty for wrong hand
DYNAMICS_WEIGHT = 0.15       // Velocity importance

// Locking
STICKY_MIN_NOTES = 4         // Notes needed to lock
STICKY_SCORE_THRESHOLD = 15  // Max score to lock
STICKY_PAUSE_MS = 2000       // Pause that unlocks
STICKY_BAD_SCORE_THRESHOLD = 50  // Score that unlocks

// Progress
LOCKED_JITTER_TOLERANCE = 2  // Allowed backward notes
LOCKED_JUMP_PENALTY_PER_NOTE = 50
PROGRESS_PENALTY = 20        // Penalty when unlocked
```

---

## Future Considerations

- [ ] Metronome with configurable subdivisions
- [ ] Recording and playback of practice sessions
- [ ] Progress tracking over time
- [ ] More drumkit patterns (jazz, Latin, etc.)
- [ ] Visual tempo guide / scrolling notation
- [ ] Audio playback of patterns
- [ ] Mobile/touch support
