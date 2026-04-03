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

## Restrictions per Stage

### Newborn & Child

| Rule | Detail |
|---|---|
| EVA | Blocked — portrait button permanently grayed out |
| Experience level | Forced to **0** (no bonuses while in vessel) |
| Science Lab / Harvester | 0 % / 25 % efficiency (see table below) |
| Caretaker | Adult crew member **required** on same vessel — death clock if absent |
| Movement (Newborn) | Warning shown when moved without an adult present |

### Teenager

| Rule | Detail |
|---|---|
| EVA | Grayed out while **no adult is on EVA within 200 m** |
| EVA with chaperone present | Allowed |
| Chaperone leaves during EVA | SAS disabled, jetpack thrust drops to **2 %** of normal |
| Experience level | Forced to **0** |
| Science Lab / Harvester | 50 % efficiency (20 % without caretaker on vessel) |

### Adult — Pregnant (first half)

| Rule | Detail |
|---|---|
| EVA | Allowed |
| Pilot SAS modes | Full |
| Science Lab / Harvester | 100 % efficiency |

### Adult — Pregnant (second half)

| Rule | Detail |
|---|---|
| EVA | Grayed out |
| Pilot experience | Forced to **level 1** — only basic SAS hold (T key), no autopilot modes |
| Science Lab / Harvester | **40 %** efficiency |

### Adult — Postpartum

| Rule | Detail |
|---|---|
| EVA | Grayed out |
| Science Lab / Harvester | **0 %** efficiency |
| Duration | 10 Kerbin days |

After postpartum ends all restrictions lift and the pilot experience level is **automatically restored** from accumulated XP.

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

| Phase | EVA | Science / Harvester | Pilot SAS |
|---|---|---|---|
| First half (days 0-15) | Yes | 100 % | Full |
| Second half (days 15-30) | No (gray) | 40 % | Level 1 only |
| Postpartum (10 days) | No (gray) | 0 % | — |

---

## Genetics

Offspring trait is determined by both parents:

| Parents | Outcome |
|---|---|
| Same + Same | 96 % same trait, 2 % each other |
| Mixed (e.g. Pilot + Scientist) | 48 % trait A, 48 % trait B, 4 % third trait |

Gender is always **50 % male / 50 % female**.

---

## Caretaker System

| Stage | Effect without caretaker | Death timer |
|---|---|---|
| Newborn | Death clock starts | **5 Kerbin days** |
| Child | Death clock starts | **15 Kerbin days** |
| Teenager | Efficiency drops to **20 %**, survives | no death |

Assigning an adult to the vessel resets the death clock immediately.

---

## Career Mode — Daily Upkeep

Non-adult Kerbals cost funds every Kerbin day:

| Stage | Cost / day |
|---|---|
| Newborn | 150 |
| Child | 100 |
| Teenager | 50 |

If funds are insufficient **LOW SUPPORT** activates:

| Stage | Normal | Low Support |
|---|---|---|
| Child | 25 % | 20 % (x 0.80) |
| Teenager (with caretaker) | 50 % | 35 % (x 0.70) |

LOW SUPPORT clears immediately once funds exceed the daily cost.

---

## Colony Manager UI

- Toolbar button available in **Flight** and **Space Centre** scenes.
- Per-vessel list with stage badge, progress bar, efficiency % and time remaining.
- **Hover** over a Kerbal name — tooltip with full stats (timers, partner, caretaker status).
- **Hover** over LOW SUPPORT badge — tooltip with penalty details and fix advice.
- Pairing panel (manual mode): male / female dropdowns + **Start Pregnancy** button.

---

## Configuration — BreedTheKerbal.cfg

No recompile needed — edit the file and restart KSP.
All durations in **Kerbin seconds** (1 Kerbin day = 21 600 s).

| Key | Default | Description |
|---|---|---|
| `PregnancyDuration` | 648000 (30 d) | Length of pregnancy |
| `PostpartumDuration` | 216000 (10 d) | Postpartum recovery |
| `NewbornDuration` | 1080000 (50 d) | Time as Newborn |
| `ChildDuration` | 2160000 (100 d) | Time as Child |
| `TeenagerDuration` | 3240000 (150 d) | Time as Teenager |
| `NewbornDeathTimer` | 108000 (5 d) | Newborn death without caretaker |
| `ChildDeathTimer` | 324000 (15 d) | Child death without caretaker |
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
