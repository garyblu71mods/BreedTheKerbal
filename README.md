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
                           +-> Newborn boards the vessel automatically
                               (loaded or unloaded — both handled)
                               If no free seat: stays Available in Astronaut Complex
```

| Phase | EVA | Efficiency | Pilot SAS |
|---|---|---|---|
| First half (days 0-15) | Yes | 100% | Full |
| Second half (days 15-30) | No | 40% | Level 1 only |
| Postpartum (10 days) | No | 0% | — |

### Birth on an unloaded vessel

If a birth occurs while the mother's vessel is **not the active vessel**, the newborn is placed
directly into the vessel's `ProtoVessel` (no free seat required to be present in IVA).
When you later **switch to that vessel**, the newborn is already on board.

If the newborn ended up in the Astronaut Complex due to an older save, switching to the
vessel where the mother is will **automatically re-board** the child.

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

## Aging Speed Bonuses

Non-adult Kerbals accumulate age at a speed that can be raised or lowered by vessel conditions.
The **Colony Manager** shows the net modifier in the `+X%` column next to each Kerbal.
Click the `+X%` label to open a per-factor breakdown popup.

| Condition | Modifier |
|---|---|
| Each Cupola Module on vessel (max 3) | +5% per cupola |
| Vessel landed or splashed | +10% |
| Active CommNet connection to home | +2% |
| Scientist as best caretaker-adult on vessel | +5% |
| Engineer as best caretaker-adult on vessel | +3% |
| Both biological parents on same vessel | +5% |
| No biological parents on vessel | −5% |
| Crew count > 75% of vessel capacity | −5% |
| Low Support active | −10% |

Modifiers are **additive**. A negative total slows aging below base rate.
All thresholds and values are configurable in `BreedTheKerbal.cfg`.

---

## Colony Manager UI

- Toolbar button: **Flight** and **Space Centre** scenes.
- Per-vessel list with stage badge, progress bar, **efficiency %** (orange = penalty, red = 0%), time remaining, and aging speed bonus column.

### Click interactions

| Click target | Result |
|---|---|
| **Kerbal name** | Popup — parents' names, trait, experience stars |
| **+X% / ±0%** aging label | Popup — per-factor aging speed breakdown with total |
| Clicking outside a popup | Closes the popup |

### Hover interactions

| Hover target | Result |
|---|---|
| **Kerbal name** | Tooltip — stage, trait, stars, efficiency, active penalty, caretaker death countdown |
| **LOW SUPPORT** badge | Tooltip — penalty breakdown and how to fix |

- Pairing panel (manual mode): male / female selectors + **Start Pregnancy** button.

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
| `CupolaAgingBonusPerUnit` | 0.05 | Aging bonus per Cupola Module (+5%) |
| `CupolaAgingMaxCount` | 3 | Maximum cupolas counted toward the bonus |
| `LandedAgingBonus` | 0.10 | Bonus when landed or splashed (+10%) |
| `CommNetBonus` | 0.02 | Bonus when connected to home via CommNet (+2%) |
| `BothParentsAgingBonus` | 0.05 | Bonus when both parents are on board (+5%) |
| `NoParentsAgingPenalty` | 0.05 | Penalty when no parents are on board (−5%) |
| `ScientistCaretakerBonus` | 0.05 | Bonus for Scientist as best adult caretaker (+5%) |
| `EngineerCaretakerBonus` | 0.03 | Bonus for Engineer as best adult caretaker (+3%) |
| `OvercrowdingThreshold` | 0.75 | Crew/capacity ratio above which penalty applies |
| `OvercrowdingPenalty` | 0.05 | Penalty when vessel is overcrowded (−5%) |
| `LowSupportAgingPenalty` | 0.10 | Aging speed penalty when Low Support is active (−10%) |

---

## Debug Shortcuts (Debug builds only)

| Shortcut | Action |
|---|---|
| Alt + Shift + B | Print all Kerbal life-stage statuses to screen |
| Alt + Shift + N | Advance all non-adult Kerbals one stage forward |

---

## Changelog

### v1.0.1 (pending release)
- **Fix:** Newborn now boards correctly when the mother's vessel was unloaded at birth (`ProtoPartSnapshot.seatIdx` fix).
- **Fix:** Retroactive boarding — switching to the vessel where the mother lives auto-boards any newborn that previously ended up in the Astronaut Complex.

### v1.0.0-beta
- Initial public beta release.
- Full life-stage system: Newborn / Child / Teenager / Adult.
- Pregnancy and postpartum mechanics.
- Caretaker death timers.
- Aging speed bonus system (9 modifiers).
- Career mode daily upkeep with Low Support fallback.
- Parents & aging breakdown popups.
- AutoBreeding option.
- Custom toolbar icon (`BTK_icon.png`).

---

## License

MIT — free to use, modify and redistribute with attribution.
