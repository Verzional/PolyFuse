# Project Specification: PolyFuse

---

## 1. Executive Summary & High Concept

* **Project Title:** PolyFuse
* **Genre:** Endless Spatial Strategy / Polyform Placement Puzzle
* **Target Platforms:** Mobile (iOS / Android Portrait) & Desktop (Mac / PC / WebGL)
* **Engine:** Unity (2D)
* **Core Inspiration:** *Block Blast* (Retention loop, combo momentum, escalating sensory juice) $\times$ Archimedean Polyform Geometry.
* **Elevator Pitch:** An endless, turn-based spatial placement puzzle where players fit distinct geometric shapes—**Triangles, Diamonds, Trapezoids, and Hexagons**—into a single unified isometric canvas. Scoring is driven by a 3-Axis line-clearing engine backed by an escalating turn-by-turn combo multiplier ("The Greed Engine") and a 3-piece grace buffer.

---

## 2. Core Gameplay Mechanics

```
┌────────────────────────────────────────────────────────────────────────┐
│                          1. The Hand (Tray)                            │
│    Spawns 3 solvable polyform pieces (1 to 6 units) via Lookahead      │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        2. Placement & Snapping                         │
│       Drag & drop onto empty triangular slots on the isometric grid    │
│    Pre-Snap Line Anticipation: Glows lines about to clear on hover     │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        3. 3-Axis Line Clearing                         │
│   Horizontal Axis (—) | Diagonal Slash (/) | Diagonal Backslash (\)    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   4. Combo Escalation / Game Over                      │
│   Clear Triggered: Combo +1, Grace Refills to 3, Melodic Note Plays    │
│   No Clear: Grace -1 (Drops after 3 turns without clears)              │
│   Tray Empty: Deal 3 new pieces | No Valid Placements: Game Over       │
└────────────────────────────────────────────────────────────────────────┘
```

### The Isometric Polyform Canvas (Radius 3 - 54 Tiles)

The board is an isometric canvas composed of interlocking equilateral triangular slots ($60^\circ$ interior angles). Triangles alternate orientation between Up ($\blacktriangle$) and Down ($\blacktriangledown$).
* **Radius:** $R = 3$ (6 rows, $r \in [0, 5]$).
* **Row Spans:** `halfWidths = [3, 4, 5, 5, 4, 3]` producing smooth, flat hexagonal perimeter boundaries (7, 9, 11, 11, 9, 7 tiles per row; total 54 tiles).

### The 9 Polyform Unit Shapes (Macro Pieces)

| Shape Name | Unit Value | Geometric Composition | Strategic Role |
| --- | --- | --- | --- |
| **The Shard** | 1 Unit | $1\times$ Single Triangle ($\blacktriangle$ or $\blacktriangledown$) | Precision gap-filler; completes lines and saves runs. |
| **The Blade** | 2 Units | $2\times$ Triangles sharing an edge (Diamond / Rhombus) | Fast diagonal bridging; sets up multi-line intersections. |
| **The Cleaver** | 3 Units | $3\times$ Triangles in a row (Trapezoid strip) | High-volume board filler; requires structural foresight. |
| **The Chevron** | 4 Units | $4\times$ Triangles forming an angled V/L Boomerang | Bridges 2 isometric axes simultaneously; rapid row completion. |
| **The Grand Cleaver** | 4 Units | $4\times$ Triangles in a continuous linear strip | Powerful straight-line coverage; fills $>50\%$ of an entire row. |
| **The Delta** | 4 Units | $4\times$ Triangles forming a $2\times 2$ Equilateral Triangle | Dense 2D corner reinforcement; anchors central clusters. |
| **The Trifork** | 4 Units | $4\times$ Triangles forming a 3-way Y-Star Propeller | Instant 3-axis trigger radiating from a central vertex. |
| **The Crown** | 5 Units | $5\times$ Triangles forming a Hexagon minus 1 triangle | High surface area; hugs perimeter curves and sets up multi-cleaves. |
| **The Core** | 6 Units | $6\times$ Triangles forming a regular Hexagon | High risk, massive footprint; strategic macro filler. |

---

### The 3-Axis Line-Clearing System

Board clearing is evaluated across all 3 isometric grid axes:
1. **Horizontal Axis (`—`):** Full horizontal rows.
2. **Diagonal Slash Axis (`/`):** $+60^\circ$ diagonal lines ($\ge 3$ tiles).
3. **Diagonal Backslash Axis (`\`):** $120^\circ$ diagonal lines ($\ge 3$ tiles).

---

## 3. Retention Architecture: The Greed Engine & Arcade Jackpot Economy

* **Exponential Jackpot Scoring Curve:**
  * **Piece Placement:** $100 \times \text{UnitCount} \times \max(1, \text{ComboStreak})$.
  * **1 Line Clear:** $1,000 \times \max(1, \text{ComboStreak})$.
  * **2 Lines Clear (Double Cleave):** $3,500 \times \max(1, \text{ComboStreak})$.
  * **3 Lines Clear (The Trifecta):** $10,000 \times \max(1, \text{ComboStreak})$.
  * **4+ Lines Clear (Super Nova):** $25,000 \times \max(1, \text{ComboStreak})$.
  * **Board Wipe Jackpot:** $+25,000 \times \max(1, \text{ComboStreak})$ bonus points.
* **Multi-Line Cleave & Board Wipe Grace Buffer:**
  * Standard 1-line clears grant a **3-turn grace buffer** (`▲ ▲ ▲`).
  * Achieving a **Multi-Line Clear** ($\ge 2$ lines: Double Cleave, Trifecta, Super Nova) grants an extended **5-turn grace buffer** (`▲ ▲ ▲ ▲ ▲`).
  * Achieving a full **Board Wipe** grants an ultimate **7-turn grace buffer** (`▲ ▲ ▲ ▲ ▲ ▲ ▲`), completely eliminating the winner's curse and providing a spacious 7-piece runway to build momentum on a blank canvas.
* **Live Run Statistics Tracking:**
  * Tracks live session metrics: **Max Combo Streak**, **Total Lines Cleared**, and **Total Pieces Placed**.
* **Escalating Combo Hype Tiers:**
  * $2\times$: `COMBO ×2!` *(Warm Gold)*
  * $3\times$: `GREAT! ×3` *(Electric Amber)*
  * $4\times$: `AMAZING! ×4` *(Neon Coral)*
  * $5\times$: `UNSTOPPABLE! ×5` *(Vibrant Magenta)*
  * $6\times$: `INCREDIBLE! ×6` *(Electric Cyan)*
  * $7\times+$: `POLYFUSE GOD! ×N` *(Prismatic Purple)*

---

## 4. Technical Specifications & Data Architecture (Unity 2D)

### A. Grid Coordinate System

The grid is modeled as a 2D matrix `grid[row, col]`:
* **Parity Check:** If `((row + col) % 2 == 0)` $\to$ Triangle points Up ($\blacktriangle$). Else $\to$ Triangle points Down ($\blacktriangledown$).
* **World Space Conversion:**

$$\text{Position.x} = \text{col} \times \left(\frac{\text{Width}}{2}\right)$$

$$\text{Position.y} = (\text{row} - \text{Radius}) \times \text{Height} + (\text{isUp} ? \frac{\text{Height}}{3} : \frac{2 \times \text{Height}}{3})$$

### B. Procedural Piece Generation (Lookahead Solver & Adaptive Parity)

* **Full 3-Piece Lookahead Solver (`IsBatchFullySolvable`):**
  * Evaluates all 6 permutations of the 3 candidate pieces on a virtual board simulation (`VirtualBoard`).
  * Accounts for lines cleared by early pieces opening up space for later pieces. Batches are only dealt if 100% solvable.
* **Adaptive Parity Balancer:**
  * Tracks live empty Up ($\blacktriangle$) vs. Down ($\blacktriangledown$) tile counts, dynamically biasing spawned shapes toward the needed orientation to maintain board equilibrium.
* **Line-Completer Emergency Rescue Mode:**
  * When fill ratio $\ge 45\%$, detects the nearest-to-complete line and synthesizes the exact missing shape (Shard, Blade, Cleaver) to guarantee a line clear.

### C. Mobile Resolution & Camera Adapter

* **`MobileResolutionAdapter.cs`:** Dynamically scales `Camera.main.orthographicSize` to fit target world width across any mobile portrait aspect ratio (9:16, 9:19.5, iPad, WebGL).
* **Native uGUI (`GameUI.cs`):** Built with zero external asset dependencies; CanvasScaler set to Match Width (`matchWidthOrHeight = 0.0f`).

---

## 5. Sensory Feedback & "Juice" Palette

| Event | Visual FX | Audio FX | Tactile / Feel |
| --- | --- | --- | --- |
| **Hover Preview** | **Pre-Snap Line Anticipation Glow:** Pulse/aura across lines that will clear. | — | Subtle drag elevation + Snap magnetism. |
| **Piece Snap** | Elastic squash-and-stretch pop ($1.18\times \to 0.92\times \to 1.0\times$). | Crisp wooden/marble click. | Light Haptic pulse (`16ms`). |
| **Line Clear** | Glowing triangle particle shatter burst + white flash. | Ascending Pentatonic Scale note ($C_4 \to D_4 \to E_4 \dots$). | Medium Haptic tick (`38ms`). |
| **Multi-Line Clear** | Screen shake + Axis laser cleave. | Layered crystal arpeggio + sub-bass boom. | Heavy Haptic buzz (`65ms`) + Hit-Stop ($0.06\text{s}$). |
| **Score Gain** | Smooth cubic ease-out roll-up ticker ($0.20\text{s} - 0.32\text{s}$) + score punch. | Ascending score tick audio. | Rhythmic score feel. |
| **Danger Mode ($\ge 65\%$)** | Screen edge crimson pulse vignette + outer coral halo pulse. | Rhythmic 62.5 BPM "lub-dub" harmonic heartbeat loop. | Escalating turn-by-turn tension. |
| **Close Call Escape** | "HEROIC CLEAR!" pop banner + cyan flash surge + confetti burst. | Triumphant brass major triad fanfare. | Massive adrenaline relief. |
| **New High Score** | "★ NEW BEST! ★" golden badge pop + 24-shard starburst. | Sparkling D-major chime arpeggio fanfare. | Golden HUD celebration. |
| **Invalid Drop** | Elastic spring return with cubic overshoot ($+9.4\%$). | Subtle soft thud. | Bouncy tray recovery. |
| **Board Wipe** | Fullscreen celebration banner + starburst. | Victorious major triad fanfare. | Heavy double shake. |

---

## 6. Retention Architecture & UI Modals

* **Dynamic Danger Loop:**
  * When board fill reaches $\ge 65\%$ (approx. 35 tiles occupied out of 54), Danger Mode initiates with a pulsing heartbeat loop and radial edge vignette.
  * Clearing lines down below $50\%$ triggers the **"HEROIC CLEAR!"** escape reward banner and fanfare.
* **Live High Score Chase:**
  * Tracks high score in real-time, triggering a mid-game golden starburst explosion the moment the player sets a personal record.
* **Minimalist Pause & Settings Modal:**
  * Procedurally generated anti-aliased gear icon in top-right HUD.
  * Floating, borderless vertical list (`PAUSED`) with $520\times 84\text{px}$ dark glass option pills.
  * Toggles for Procedural Sound FX and Haptics with `PlayerPrefs` cross-session persistence.
  * Smooth $0.16\text{s}$ ease-out scale and alpha transition.
* **Arcade Glass Card Game Over Modal:**
  * Dedicated floating dark obsidian glass card ($600\times 600\text{px}$).
  * Huge $74\text{px}$ bold white DIN score digits.
  * **Run Stats Breakdown Bar:** 3-cell inset displaying **MAX COMBO**, **LINES**, and **PIECES**.
  * Gold high score pill comparison (`★ BEST: X,XXX` or `★ NEW BEST RECORD! ★`).
  * Electric Cyan `▶ PLAY AGAIN` hero action button.
  * Automatic Top HUD auto-occlusion during Game Over to eliminate duplicate number competition.

---
