# BreedTheKerbal

A KSP 1.x mod that adds a full Kerbal life-cycle — pregnancy, birth, childhood and aging —
with real gameplay consequences for EVA, piloting, science labs, resource harvesters,
career upkeep costs and crew caretaking.

---

## Requirements

| Item | Version / Note |
|---|---|
| KSP | 1.x (tested on 1.12) |
| Game mode | All — Career-specific features activate only in Career mode |
| Breeding vessel | Must have a **Science Lab** AND a part with **crew capacity >= 4** |

---

## Installation

1. Build the project (Visual Studio) or download a release `.dll`.
2. Copy `BreedTheKerbal.dll` **and** `BreedTheKerbal.cfg` into `GameData/BreedTheKerbal/`.
3. Launch KSP — the **Colony Manager** toolbar button appears in the Flight and Space Centre scenes.

---

## Life Stages

Every Kerbal born through the mod passes through four stages.

```
[Born] -> Newborn (50 days) -> Child (100 days) -> Teenager (150 days) -> Adult
```

All durations are in **Kerbin days** (1 Kerbin day = 6 h real-time).
They can be changed in `BreedTheKerbal.cfg` without recompiling.

---

## All Penalties at a Glance

| State | EVA | Science Lab / Harvester | Pilot | Notes |
|---|---|---|---|---|
| Newborn | BLOCKED | 0% | 0 stars | Death clock without caretaker (5 d) |
| Child | BLOCKED | 25% | 0 stars | Death clock without caretaker (15 d) |
| Child — Low Support | BLOCKED | **20%** | 0 stars | Insufficient career funds |
| Teenager (with caretaker) | OK | 50% | 0 stars | — |
| Teenager (no caretaker) | BLOCKED | **20%** | 0 stars | EVA button grays out |
| Teen EVA — chaperone leaves | OK (already out) | — | — | **SAS off, jetpack 2%** |
| Pregnant — first half | OK | 100% | Full | No penalty |
| Pregnant — second half | BLOCKED | **40%** | **Level 1 only** (basic SAS T-key) | — |
| Postpartum | BLOCKED | **0%** | — | 10-day recovery |

> The Colony Manager tooltip (hover over a Kerbal name) shows the active penalty in orange/red
> and a live countdown to death when a Newborn or Child has no caretaker.

---

## Restrictions — Detail

### Newborn & Child

- EVA portrait button is **permanently grayed out** (G-force blackout mechanism).
- Experience level forced to **0** — no pilot SAS modes, no engineer/scientist bonuses.
- Caretaker (adult crew member on same vessel) **required**.
  - Newborn: dies after **5 Kerbin days** without caretaker.
  - Child: dies after **15 Kerbin days** without caretaker.
  - Countdown visible in Colony Manager tooltip.
- Moving a Newborn without an adult on the vessel triggers a warning.

### Teenager

- EVA button **grayed out** when no adult is on EVA within 200 m.
- When a chaperone IS on EVA the button becomes available.
- If the chaperone **boards back** while the teen is on EVA:
  - SAS is disabled immediately.
  - Jetpack thrust drops to **2%** of normal (translation still works).
- Experience level forced to **0**.
- Efficiency 50% with caretaker on vessel, 20% without.

### Pregnant — second half

- EVA button grayed out.
- Pilot experience capped at **level 1** (T-key SAS hold only, no prograde/retrograde modes).
- Science lab and harvester efficiency: **40%**.
- After birth the experience level is **automatically restored** from accumulated XP.

### Postpartum (10 days)

- EVA button grayed out.
- Science lab and harvester efficiency: **0%**.

---

## Breeding

### Vessel requirements
- Part with **crew capacity >= 4** (large habitat)
- **Science Lab** present
- At least one **adult male** and one **adult female** (not pregnant / postpartum)
- At least **one free seat** (auto-reserved during pregnancy)

### Manual mode (default)
Open **Colony Manager** -> select a male and female -> click **Start Pregnancy**.

### Auto mode
Set `AutoBreeding = true` in `BreedTheKerbal.cfg`.
Any loaded vessel meeting all conditions will automatically pair once per update cycle.

---

## Pregnancy -> Birth -> Postpartum

```
Pregnant --(30 days)--> Birth --(10 days postpartum)--> Adult again
                           |
                           +-> New Kerbal spawned on vessel (or Available if no free seat)
```

| Phase | EVA | Efficiency | Pilot SAS |
|---|---|---|---|
| First half (days 0-15) | Yes | 100% | Full |
| Second half (days 15-30) | No | 40% | Level 1 only |
| Postpartum (10 days) | No | 0% | — |

---

## Genetics

| Parents | Outcome |
|---|---|
| Same + Same | 96% same trait, 2% each other |
| Mixed (e.g. Pilot + Scientist) | 48% trait A, 48% trait B, 4% third trait |

Gender is always **50% male / 50% female**.

---

## Caretaker System

| Stage | No caretaker — effect | Death timer |
|---|---|---|
| Newborn | Death clock starts + visible in Colony Manager tooltip | **5 Kerbin days** |
| Child | Death clock starts + visible in Colony Manager tooltip | **15 Kerbin days** |
| Teenager | Efficiency 20% instead of 50%, EVA button grays out | no death |

---

## Career Mode — Daily Upkeep

| Stage | Cost / day |
|---|---|
| Newborn | 150 funds |
| Child | 100 funds |
| Teenager | 50 funds |

If funds are insufficient **LOW SUPPORT** activates:

| Stage | Normal | Low Support |
|---|---|---|
| Child | 25% | 20% (x 0.80) |
| Teenager (with caretaker) | 50% | 35% (x 0.70) |

LOW SUPPORT clears immediately once funds exceed the daily cost.

---

## Colony Manager UI

- Toolbar button: **Flight** and **Space Centre** scenes.
- Per-vessel list with stage badge, progress bar, **efficiency %** (orange = penalty, red = 0%), and time remaining.
- **Hover over a Kerbal name** — tooltip shows:
  - Stage, trait, experience stars, current efficiency
  - Active penalty description in **orange** (e.g. "Child: EVA blocked, 25% efficiency")
  - **Caretaker death countdown in red** if Newborn/Child has no adult on vessel
  - Time until next life stage / birth / recovery
- **Hover over LOW SUPPORT badge** — shows penalty breakdown and how to fix.
- Pairing panel (manual mode): male / female dropdowns + **Start Pregnancy** button.

---

## Configuration — BreedTheKerbal.cfg

No recompile needed — edit and restart KSP.
All durations in **Kerbin seconds** (1 Kerbin day = 21 600 s).

| Key | Production default | Description |
|---|---|---|
| `PregnancyDuration` | 648000 (30 d) | Length of pregnancy |
| `PostpartumDuration` | 216000 (10 d) | Postpartum recovery |
| `NewbornDuration` | 1080000 (50 d) | Time as Newborn |
| `ChildDuration` | 2160000 (100 d) | Time as Child |
| `TeenagerDuration` | 3240000 (150 d) | Time as Teenager |
| `NewbornDeathTimer` | 108000 (5 d) | Newborn dies without caretaker after this |
| `ChildDeathTimer` | 324000 (15 d) | Child dies without caretaker after this |
| `LatePregnancyEfficiency` | 0.40 | Efficiency — second half of pregnancy |
| `PostpartumEfficiency` | 0.00 | Efficiency during postpartum |
| `NewbornEfficiency` | 0.00 | Newborn efficiency |
| `ChildEfficiency` | 0.25 | Child efficiency |
| `TeenagerEfficiency` | 0.50 | Teenager efficiency (with caretaker) |
| `NoCaretakerTeenagerEfficiency` | 0.20 | Teenager efficiency (no caretaker) |
| `LowSupportChildFactor` | 0.80 | Low Support multiplier for Child |
| `LowSupportTeenagerFactor` | 0.70 | Low Support multiplier for Teenager |
| `NewbornDailyCost` | 150 | Career funds / Kerbin day (Newborn) |
| `ChildDailyCost` | 100 | Career funds / Kerbin day (Child) |
| `TeenagerDailyCost` | 50 | Career funds / Kerbin day (Teenager) |
| `HabitatMinCrewCapacity` | 4 | Minimum part crew capacity for habitat |
| `AutoBreeding` | false | Enable automatic pairing |

---

## Debug Shortcuts (Debug builds only)

| Shortcut | Action |
|---|---|
| Alt + Shift + B | Print all Kerbal life-stage statuses to screen |
| Alt + Shift + N | Advance all non-adult Kerbals one stage forward |

---

## License

MIT — free to use, modify and redistribute with attribution.
