# Project Specification: PolyFuse

---

## 1. Executive Summary & High Concept

* **Project Title:** PolyFuse
* **Genre:** Endless Spatial Strategy / Polyform Placement Puzzle
* **Target Platforms:** Mobile (iOS / Android) & Desktop (Mac / PC / WebGL)
* **Engine:** Unity (2D)
* **Core Inspiration:** *Block Blast* (Retention loop, combo momentum, escalating sensory juice) $\times$ Archimedean Polyform Geometry.
* **Elevator Pitch:** An endless, turn-based spatial placement puzzle where players fit distinct geometric shapes—**Triangles, Diamonds, Trapezoids, and Hexagons**—into a single unified isometric canvas. Scoring is driven by a dual-clearing engine (3-Axis Straight Lines + 6-Unit Hex Core Implosions) backed by an escalating turn-by-turn combo multiplier ("The Greed Engine").

---

## 2. Core Gameplay Mechanics

```
┌────────────────────────────────────────────────────────────────────────┐
│                          1. The Hand (Tray)                            │
│           Spawns 3 randomized polyform pieces (1 to 6 units)           │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        2. Placement & Snapping                         │
│       Drag & drop onto empty triangular slots on the isometric grid    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                      3. Dual-Clear Evaluation                          │
│   Check A: 3-Axis Lines (—, /, \)     Check B: 6-Unit Hex-Core Implosion │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   4. Combo Escalation / Game Over                      │
│   Clear Triggered: Combo Multiplier +1, Pitch Scales Up, Board Wipes   │
│   No Clear: Combo resets to 0                                          │
│   Tray Empty: Deal 3 new pieces | No Valid Placements: Game Over       │
└────────────────────────────────────────────────────────────────────────┘

```

### The Isometric Polyform Canvas

The board is an isometric canvas composed of interlocking equilateral triangular slots ($60^\circ$ interior angles). Triangles alternate orientation between Up ($\blacktriangle$) and Down ($\blacktriangledown$).

### The 4 Unit Shapes (Macro Pieces)

| Shape Name | Unit Value | Geometric Composition | Strategic Role |
| --- | --- | --- | --- |
| **The Shard** | 1 Unit | $1\times$ Single Triangle ($\blacktriangle$ or $\blacktriangledown$) | Precision gap-filler; saves runs from tight dead-ends. |
| **The Blade** | 2 Units | $2\times$ Triangles sharing a base edge (Diamond / Rhombus) | Fast diagonal bridging; sets up multi-line intersections. |
| **The Cleaver** | 3 Units | $3\times$ Triangles in a row (Trapezoid / Chevron) | High-volume board filler; requires structural foresight. |
| **The Core** | 6 Units | $6\times$ Triangles forming a complete regular Hexagon | High risk, massive footprint; triggers instant board wipe. |

---

### The Dual-Clearing Win State

1. **3-Axis Line Clears:** Completing an unbroken line of occupied triangles across any of the 3 isometric axes:
* **Horizontal Axis (`—`):** Full horizontal row.
* **Diagonal Up Axis (`/`):** $60^\circ$ diagonal row.
* **Diagonal Down Axis (`\`):** $120^\circ$ diagonal row.


2. **Hex-Core Detonation:** Completely filling all 6 triangular segments of any pre-defined Hexagonal sub-cell.
* *Payoff:* The core implodes radially, clearing all 6 internal units and granting a high-value flat point bonus + screen shockwave.



---

## 3. Retention Architecture: The Greed Engine

* **Turn-by-Turn Combo Streak:**
* Placing a piece that triggers $\ge 1$ clear increments the combo counter ($1\times \to 2\times \to 3\times \dots \to N\times$).
* Placing a piece that does **not** trigger a clear resets the combo counter to $0\times$.


* **The Push-Your-Luck Dilemma:** Players are incentivized to intentionally crowd the board to stage massive multi-line / core intersections rather than taking safe, single-line clears.
* **Flawless Death Attribution:** Turn-based pacing gives players 100% agency. Game overs are directly attributed to player greed rather than reflex failure.

---

## 4. Technical Specifications & Data Architecture (Unity 2D)

### A. Grid Coordinate System

The grid is modeled as a 2D matrix `grid[row, col]`:

* **Parity Check:** If `(row + col) % 2 == 0` $\to$ Triangle points Up ($\blacktriangle$). Else $\to$ Triangle points Down ($\blacktriangledown$).
* **World Space Conversion:**

$$\text{Position.x} = \text{col} \times \left(\frac{\text{Width}}{2}\right)$$


$$\text{Position.y} = \text{row} \times \text{Height}$$


* **Data per Cell:**
```csharp
public class TriangleTile : MonoBehaviour {
    public int row;
    public int col;
    public bool isPointingUp;
    public bool isOccupied;
    public int hexCoreID; // ID of the 6-unit hexagon this tile belongs to
}

```



### B. Procedural Piece Generation (Anti-Softlock Bag System)

To ensure long runs remain fair:

* **The Hand:** Always spawns in batches of 3 pieces.
* **Solver Validation:** Before presenting a 3-piece batch, the generator runs a quick board check: at least 1 of the 3 generated pieces **must** have a valid coordinate placement on the current board state.
* **Weight Distribution:**
* 1–2 Unit Pieces (Shard, Blade): ~50%
* 3 Unit Pieces (Cleaver): ~35%
* 6 Unit Pieces (Hex Core): ~15%



---

## 5. Sensory Feedback & "Juice" Palette

| Event | Visual FX | Audio FX | Haptic / Feel |
| --- | --- | --- | --- |
| **Piece Snap** | Squash-and-stretch tween ($1.1\times \to 1.0\times$). | Crisp wooden/stone click. | Light haptic tick. |
| **Line Clear** | White laser cleave along the axis; tiles shatter outward. | Sharp blade/glass chime. | Medium haptic pop. |
| **Hex Core Implosion** | Inward compression $\to$ radial chromatic shockwave. | Deep bass boom + metallic resonant ring. | Heavy dual rumble. |
| **Combo Ladder** | Floating animated multiplier badge ($+300 \times 4$). | Sound pitch scales up by $+0.12$ per streak step. | Continuous micro-rumble. |
| **Hit-Stop** | Freeze `Time.timeScale = 0f` for $0.05\text{s}$ on multi-clears. | Audio low-pass filter during freeze. | Tactile impact pause. |

---