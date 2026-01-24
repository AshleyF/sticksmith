# Sticksmith

A web-based practice tool for drummers. Sticksmith listens to your MIDI drum input and matches your playing against rudiment patterns in real-time, providing visual feedback on timing, dynamics, and sticking accuracy.

## Features

- **Pattern Matching**: Automatically detects which rudiment you're playing from a library of patterns (paradiddles, five stroke rolls, etc.)
- **Visual Notation**: Displays the current pattern with a cursor showing your position
- **Performance Metrics**: Tracks timing accuracy, dynamics consistency, and sticking correctness
- **Sticky Locking**: Once you're playing a pattern well, it locks on and follows your progress through the pattern

## Usage

Open `monitor.html` in a browser with Web MIDI support (Chrome recommended). Connect a MIDI drum pad or electronic kit, configure your stick mapping (which MIDI notes correspond to left/right hand), and start playing.

---

## To Do

### Gap Click Exercise
A metronome training mode where the click periodically goes silent for a set number of bars. You continue playing to keep time internally, then the click returns and you're scored on how well you stayed in sync.

### Timing Graph
A visual display with rows for each instrument showing timing deviation over time. A center band represents "in the pocket" - deviations to the right indicate rushing, to the left indicate dragging. Helps identify timing tendencies on specific instruments.
