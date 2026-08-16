# Solarpunk UI/UX Design System: Chlorophyll OS & VerdantAgent

**Version:** 3.5 — Biophilic Terminal & Hardware Architecture

**Focus:** Terminal-Native Glyphs, Text Glyphs, Symbiotic Coding Agents & Ecological Harmony

---

## 1. Vision & Core Philosophy

In a thriving Solarpunk future, technology is not cold, sterile, neon-cyberpunk, or hyper-industrialised. Computation breathes in rhythm with the sun, soil, and community. Digital agents serve as digital arborists and symbiotic co-creators rather than opaque command feeds.

### Foundational Tenets

* **Biophilic Tactility:** Interfaces feel alive, warm, organic, and grounded across low-power e-ink, transflective monitors, and natural enclosure materials (cork, reclaimed timber, ceramic).


* **Energy-Aware & Circadian Computing:** Layout density, colour palettes, and agent telemetry adapt dynamically to microgrid battery levels and ambient natural light.


* **Symbiosis over Automation:** The AI agent acts as a mycelial collaborator—proposing root paths, scaffold branches, and shared maintenance rather than blind, opaque code generation.


* **Repairability & Open Lineage:** Component trees, AST diffs, model weights, and agent decision branches remain inspectable and community-forkable.



---

## 2. Terminal-Native Glyphs & Symbol Hierarchy

Instead of emojis, the interface utilises clean Unicode box-drawing symbols, astronomical characters, and botanical-inspired typography compatible with standard monospace programming fonts.

### A. Terminal Glyph Mapping Table

| Semantic Category | Glyph | Unicode Description | Terminal Usage |
| :--- | :--- | :--- | :--- |
| Solar / Grid State | ☼ / ☿ | Sun with Rays / Mercury Sun | Solar yield, battery peak state |
| Canopy / Workspace | ☵ / ☲ | Trigram Water / Trigram Fire | Project workspace / context |
| Root / Dependency | ⑂ / ⑃ | Branching Fork Indicators | AST branch, dependency graph |
| Seedling (File) | ⌕ / ⌗ | Geometric Node / File Mark | Module, configuration node |
| Leaf / Clean State | ❦ / ❡ | Floral Heart / Pilcrow Stem | Clean build, optimal execution |
| Spore / Suggestion | ⁖ / ⁘ | Four Dot Punctuation | AI inline suggestion / graft |
| Pruned Branch | ✁ / ⌿ | Cut Point / Slash Div | Deleted code, pruned subtree |
| Energy Flow / Mesh | ⟲ / ⇋ | Clockwise Open / Flow Arrow | Mesh network sync, peer stream |
| Anomaly / Drought | ⚠ / ⌁ | Warning / Micro-Gap Line | Thermal load, missing stream |

### B. ASCII & Line-Drawing State Indicators

* **Idle / Resting:** `[ ~ ~ ~ ]` (Low-frequency oscillation)


* **Harvesting Solar Compute:** `[ = = = > ]` (Gradual cell fill)


* **Agent Deliberation:** `[ . : : . ]` (Spore dispersion)


* **Healthy Test Run:** `[ OK / ❦ ]`

* **Root-Rot / Anomaly:** `[ ERR / ⌁ ]`


---

## 3. Colour Palette & Circadian Sensory Spectrum

Deep botanical tones, warm earthen pigments, sun-drenched terracottas, and gentle bioluminescent accents optimise clarity while avoiding blue-spectrum strain.

### A. ANSI & 24-bit TrueColour Palette

| Token Name | Hex Code | ANSI 256 | Semantic Context |
| :--- | :--- | :--- | :--- |
| Terracotta Sol | #D96B43 | 166 | Active selections, solar harvest |
| Living Canopy | #2D5A3F | 29 | Background panels, tree hierarchy |
| Algae Lumens | #73C991 | 114 | Validated builds, clean tests |
| Sunlit Ochre | #E5A93C | 178 | Grid alerts, branch divergences |
| Mycelium Linen | #F6F3EB | 231 | High-legibility foreground text |
| Spore Dust | #B8A388 | 144 | Muted labels, secondary chrome |
| Biolum Azure | #3891A6 | 31 | Local mesh connections |
| Clay Ember | #8B3A2B | 88 | Runtime exceptions, broken links |

### B. Dynamic Circadian Shifts

* **Dawn (05:00 – 08:30):** Amber mist and soft parchment tones (`#FFF7E6` to `#E5A93C`). Low contrast to awaken the eyes gently.


* **Zenith (08:30 – 17:00):** High-contrast leaf greens, warm chalk backgrounds, and sharp vegetable-black ink rendering. Optimised for direct sunlight.


* **Golden Hour (17:00 – 20:00):** Saffron, terracotta, and warm walnut tones. Blue-spectrum light is completely phased out.


* **Bioluminescent Night (20:00 – 05:00):** Peat-moss background (`#131D17`) with low-intensity pale firefly text accents (`#6BD69E`).



---

## 4. Physical & Robotics Hardware Integration

```
       ┌────────────────────────────────────────────────────────┐
       │   [ ☼ Sol-Collector Photovoltaic Trim (Brass Rim) ]    │
       │  ┌──────────────────────────────────────────────────┐  │
       │  │  ☵ VerdantAgent Core Interface (E-Ink / Micro)   │  │
       │  │  [ ⑂ Root Map ] [ ☼ Grid: 94% ] [ ⇋ Mesh: Actv ] │  │
       │  │  "Let's prune the AST parser tree together."      │  │
       │  └──────────────────────────────────────────────────┘  │
       │   ( Ceramic Dial )   [ Tactile Cork Keybed ]    ( ❦ )  │
       │      [Scrub AST]       [Direct Mechanical]   [Spore]   │
       └────────────────────────────────────────────────────────┘

```

* **Chassis Materials:** CNC-milled salvaged olive wood, terracotta ceramic dials, hemp-composite casing, and brass capacitive toggles.


* **Passive Display Screens:** Transflective e-paper with vegetable oil micro-capsules and low-energy RGB micro-OLED accents powered by ambient room light.


* **Ambient Robotics (The "Seedpod" Companion):**
* Soft pneumatic articulation covered in knitted organic wool.


* Micro-gestures: blooms open when compiling, tilts inquisitively when encountering a logic anomaly, and rests curled in a sleep loop when the user steps into the garden.─────

---

## 5. UI Architecture: The Living Greenhouse Buffer

```
+-----------------------------------------------------------------------------------+
| ☼ 2.4 kW [Battery: 88%]  |  ☵ Hydroponic-OS (v2.1)  |  ⇋ Local Mesh: 12 Nodes      |
+-----------------------------------------------------------------------------------+
| ⌕ SEEDLINGS (Files)  | ⑂ SYMBIOTIC CODE BUFFER              | ❦ AGENT ARBORIST    |
|                      |                                       |                     |
| ├── ⌗ pumps/         | 1  fn distribute_nutrient(flow: Rate) | Model: Verdant-7B   |
| │   ├── sensor.rs    | 2     -> Result<CanopyState> {        | State: [ . : : . ]  |
| │   └── valve.rs     | 3      // Symbiotic Suggestion:       |                     |
| ├── ⌗ solar_calc/    | 4  ⁖⁖⁖ mycorrhizal_stream! { ⁖⁖⁖⁖⁖⁖⁖  | "I noticed valve.rs |
| └── ⌗ ecosystem.toml | 5      let energy = grid.harvest()?;  | could drop 12mW of  |
|                      | 6      valve.pulse(energy.surplus);   | idle draw if we use |
| [ + Plant Branch ]   | 7  ⁖⁖⁖ } ⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖⁖  | async ticks."       |
| [ ✁ Prune Node ]     | 8  }                                  |                     |
|                      |                                       | [Enter] Graft Code  |
|                      |                                       | [Esc]   Prune       |
+-----------------------------------------------------------------------------------+
| ☼ Grid: 98% Local  •  ⑃ AST Depth: 14 Nodes  •  ❦ Zero External Telemetry         |
+-----------------------------------------------------------------------------------+

```

### Component Breakdown

1. **The Code "Grafts" (Diff & Inline Suggestions):** Suggestions appear as woven, translucent willow vines (`#73C991` at 20% opacity) that gently wrap around code lines.


2. **The Thought Canopy (Reasoning Visualiser):** An interactive root-and-branch diagram rendering inputs as roots, algorithmic core as the trunk, and output options as blossoms.


3. **Compute Budget Gauge:** Shows real-time energy cost per LLM token inference and highlights whether execution runs on local neural silicon (0.8W) or community solar compute barns.



---

## 6. UX Micro-Interactions & Acoustic Landscape

* **Refactoring Dissolution:** Cleaned blocks collapse into micro-dots (`. : .`) and golden pollen particles that dissolve into the line buffer.


* **Sprout Loaders:** Uncurling fern fronds and stepped growth meters calibrated to processing latency: `[ - ] -> [ -- ] -> [ --- ] -> [ ❦ ]`.


* **Acoustic Earcons:** Soft physical acoustic clicks resembling bamboo strikes (*Shishi-odoshi*), hollow woodblocks, or soft wind chimes.



---

## 7. Language & Linguistic Framework: The Mycelial Voice

Human-agent interaction replaces destructive, corporate jargon with restorative, biological terminology.

| Traditional Terminology | Solarpunk Linguistic Shift | Rationale & Context |
| :--- | :--- | :--- |
| Kill Process / Abort | Prune Branch / Let Rest | Orderly cycle recycling and deallocation |
| Force Push / Override | Harmonise Tree | Consensus-driven code integration |
| Fatal Crash / Exception | Nutrient Drought / Drift | Framing bugs as systemic imbalances |
| Master / Slave | Canopy Lattice / Co-Root | Non-hierarchical decentralised layout |
| Latency Spike | Canopy Shadow | Solar-compute throttle awareness |
| Telemetry / Tracking | Mycelial Pulse / Share | Opt-in, transparent local mesh sharing |

### Conversational Dialogue Patterns

* **Proactive Refactoring:** *"I noticed excess memory retention in `sensor.rs`. By swapping to a ring buffer, we save 14% RAM and reduce cycle draw on the battery. Shall we graft this change?"*

* **Diagnostic Catch:** *"The async data channel experienced a signal drought at line 144. Let's install a deep-root fallback so valve timing remains stable."*

* **Rest Prompt:** *"The sun is below the horizon and compute draw is shifting to reserves. Let's save our seeds to the local mesh and resume at dawn."*


---

## 8. Accessibility, Ethics & Open Stewardship

* **High-Contrast Sun-Mode:** Instant toggle for working outside under open sky with crisp monochrome e-ink contrast.


* **Community-Sourced & Forkable:** Agent memory banks can be preserved locally as Markdown/JSON seed files and shared across peer-to-peer mesh networks.


* **Zero Dark Patterns:** No artificial urgency, no engagement traps, and no background tracking. The system actively encourages human rhythm and outdoor rest.