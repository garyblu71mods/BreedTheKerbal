# BreedTheKerbal — Quick Guide

## Requirements
- KSP 1.x (tested on 1.12)
- No external dependencies — vanilla KSP only

## Installation
1. Copy `BreedTheKerbal.dll` and `BreedTheKerbal.cfg` into `GameData/BreedTheKerbal/`
2. Done — the **Colony Manager** button appears in the toolbar

---

## Getting Started

**Vessel requirements:**
- A part with crew capacity >= 4
- Science Lab
- One adult male + one adult female (not pregnant, not postpartum)
- >= 1 free seat

**Manual pairing** (default):
Colony Manager -> select a pair -> **Start Pregnancy**

**Automatic:**
Set `AutoBreeding = true` in `BreedTheKerbal.cfg`

---

## Life Stages

Birth -> Newborn (50 days) -> Child (100 days) -> Teenager (150 days) -> Adult

*(1 Kerbin day = 6 h real time)*

| Stage | EVA | Lab / Harvester | Pilot |
|---|---|---|---|
| Newborn | No | 0% | 0 stars |
| Child | No | 25% | 0 stars |
| Teenager (with caretaker) | Yes | 50% | 0 stars |
| Teenager (no caretaker) | No | 20% | 0 stars |
| Pregnant - 1st half | Yes | 100% | full |
| Pregnant - 2nd half | No | 40% | level 1 |
| Postpartum (10 days) | No | 0% | -- |

---

## Aging Speed Bonuses

Non-adult Kerbals age faster (or slower) depending on conditions.
The **+X%** column in Colony Manager shows the net modifier — click it for a full breakdown.

| Condition | Effect |
|---|---|
| Each Cupola Module on vessel (max 3) | +5% per cupola |
| Vessel landed or splashed | +10% |
| Active CommNet connection to home | +2% |
| Scientist as best adult on board | +5% |
| Engineer as best adult on board | +3% |
| Both biological parents on board | +5% |
| No biological parents on board | −5% |
| Crew > 75% of vessel capacity | −5% |
| Low Support active (career) | −10% |

*(Base aging speed = 100 %. Modifiers are additive. Negative total slows aging.)*

---

## Caretaker

Newborns and Children **must have an adult** on the same vessel.

| Stage | Without caretaker |
|---|---|
| Newborn | death after **5 days** |
| Child | death after **15 days** |
| Teenager | efficiency drops to 20%, EVA blocked |

A red bar and countdown in Colony Manager show time remaining until death.

---

## Career Mode - Daily Upkeep

| Stage | Cost / day |
|---|---|
| Newborn | 150 funds |
| Child | 100 funds |
| Teenager | 50 funds |

Insufficient funds -> **LOW SUPPORT** -> Child 20%, Teenager 35%, aging speed −10%

---

## Colony Manager

| Click target | Result |
|---|---|
| **Kerbal name** | Popup: parents, trait, experience stars |
| **+X% / ±0%** bonus label | Popup: per-factor aging speed breakdown |
| **LOW SUPPORT** badge (hover) | Tooltip: funding shortfall details |
| **Kerbal name** (hover) | Tooltip: full status, active penalty, countdown |

Popups close when clicking outside or clicking the same label again.

---

## Configuration (BreedTheKerbal.cfg)

Edit and restart KSP — no recompile needed.
All durations in Kerbin seconds (1 day = 21 600 s).

| Key | Production default | Description |
|---|---|---|
| `PregnancyDuration` | 648000 | 30 Kerbin days |
| `PostpartumDuration` | 216000 | 10 days |
| `NewbornDuration` | 1080000 | 50 days |
| `ChildDuration` | 2160000 | 100 days |
| `TeenagerDuration` | 3240000 | 150 days |
| `NewbornDeathTimer` | 108000 | 5 days without caretaker |
| `ChildDeathTimer` | 324000 | 15 days without caretaker |
| `CupolaAgingBonusPerUnit` | 0.05 | +5% aging per cupola |
| `CupolaAgingMaxCount` | 3 | Max cupolas counted |
| `LandedAgingBonus` | 0.10 | +10% when landed/splashed |
| `CommNetBonus` | 0.02 | +2% when connected to home |
| `BothParentsAgingBonus` | 0.05 | +5% with both parents |
| `NoParentsAgingPenalty` | 0.05 | −5% with no parents |
| `ScientistCaretakerBonus` | 0.05 | +5% — Scientist caretaker |
| `EngineerCaretakerBonus` | 0.03 | +3% — Engineer caretaker |
| `OvercrowdingThreshold` | 0.75 | Ratio above which penalty applies |
| `OvercrowdingPenalty` | 0.05 | −5% when overcrowded |
| `LowSupportAgingPenalty` | 0.10 | −10% aging when Low Support |
| `AutoBreeding` | false | Enable automatic pairing |

---

## Debug Shortcuts (Debug build only)

| Shortcut | Action |
|---|---|
| Alt + Shift + B | Print all Kerbal life-stage statuses to screen |
| Alt + Shift + N | Advance all non-adults one stage forward |
