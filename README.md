# BreedTheKerbal

A KSP 1.x mod that adds a full life-cycle system to your Kerbals — pregnancy, birth, childhood, and aging — with gameplay consequences for Career, Science, and Sandbox modes.

---

## Requirements

| Item | Requirement |
|---|---|
| KSP version | 1.x (tested on 1.12) |
| Game mode | All (Career features only active in Career) |
| Vessel for breeding | Must have a **Science Lab** AND at least one part with **crew capacity ≥ 4** |

---

## Installation

1. Copy `BreedTheKerbal.dll` into `GameData/BreedTheKerbal/`
2. Launch KSP — the Colony Manager button appears in the toolbar (flight scene and Space Centre)

---

## Life Stages

Every Kerbal born through the mod passes through four stages before becoming a full crew member.

```
[Born] → Newborn (50 days) → Child (100 days) → Teenager (150 days) → Adult
```

All durations are in **Kerbin days** (1 Kerbin day = 6 hours real-time).

| Stage | Duration | Can EVA? | Science Lab efficiency |
|---|---|---|---|
| **Newborn** | 50 days | ✗ | 0 % |
| **Child** | 100 days | ✗ | 25 % |
| **Teenager** | 150 days | ✓ | 50 % |
| **Adult** | — | ✓ | 100 % |

> Efficiency shown is the **multiplier applied to `dataProcessingMultiplier`** of every Science Lab on the vessel. The **minimum** efficiency across all crew members is used — one Newborn on a lab vessel tanks the whole lab.

---

## Breeding

### Requirements (per vessel)
- At least one part with **crew capacity ≥ 4** (large habitat)
- A **Science Lab** part
- At least one **adult male** and one **adult female**
- The female must not be pregnant or postpartum
- At least **one free seat** on the vessel (reserved automatically during pregnancy)

### Manual breeding (default)
Open the **Colony Manager** window → select a male and a female → click **Start Pregnancy**.  
The button shows reasons if conditions are not met.

### Auto breeding
Disabled by default. Can be enabled by setting `AutoBreeding = true` in `BreedingConfig.cs`.  
When enabled, any loaded vessel meeting all conditions will automatically start a pregnancy every cycle.

---

## Pregnancy & Postpartum

| Phase | Duration | Efficiency penalty |
|---|---|---|
| Pregnant | 30 days | −30 % (70 % efficiency) |
| Postpartum | 10 days | −50 % (50 % efficiency), no EVA |

At the end of pregnancy a new Kerbal is **spawned directly on the vessel** if a free seat exists. If the vessel is full, the newborn is placed in the astronaut complex roster as Available.

---

## Genetics

Offspring trait (Pilot / Scientist / Engineer) is determined by both parents:

| Parents | Result |
|---|---|
| Same + Same | 96 % same trait, 2 % each other trait |
| Mixed (e.g. Pilot + Scientist) | 48 % trait A, 48 % trait B, 4 % third trait |

Gender is always **50 % male / 50 % female**, independent of parents.

---

## Caretaker System

Non-adult Kerbals on a vessel need at least one **adult crew member** present as a caretaker.

| Stage | Effect without caretaker | Death timer |
|---|---|---|
| Newborn | Death clock starts | **5 days** |
| Child | Death clock starts | **15 days** |
| Teenager | Survives, but efficiency drops to 20 % | — (no death) |

If a caretaker is assigned to the vessel the death clock resets to zero immediately.

---

## Career Mode — Daily Upkeep

In Career mode, non-adult Kerbals cost funds every Kerbin day.

| Stage | Cost per day |
|---|---|
| Newborn | 150 funds |
| Child | 100 funds |
| Teenager | 50 funds |
| Adult | 0 funds |

If funds are **insufficient** the **LOW SUPPORT** state activates:

| Stage | Normal efficiency | Low Support efficiency |
|---|---|---|
| Child | 25 % | 20 % (×0.80) |
| Teenager (with caretaker) | 50 % | 35 % (×0.70) |

LOW SUPPORT clears **immediately** when your funds exceed the daily upkeep cost — you do not need to wait until the next payment cycle.

---

## Colony Manager UI

Open via the toolbar button (rocket icon). Available in Flight and Space Centre.

- **Per-vessel sections** — shows every tracked Kerbal with stage badge, progress bar, efficiency %, and time remaining
- **Hover over a Kerbal name** — tooltip with full stats: stage, timers, partner, caretaker status
- **Hover over LOW SUPPORT badge** — tooltip explaining the penalty and how to fix it
- **Pairing panel** — male/female dropdowns + Start Pregnancy button (manual mode)
- **Header** — shows daily upkeep cost and LOW SUPPORT badge when active

---

## What Affects What — Quick Reference

```
Vessel has Science Lab + large habitat (cap ≥ 4)
    └─ Enables breeding (manual or auto)
    └─ Science Lab efficiency is modified by crew life stages

Crew life stage
    └─ Newborn/Child → no EVA
    └─ Postpartum female → no EVA
    └─ Non-adult on lab vessel → reduces dataProcessingMultiplier
          (minimum across all crew is used)

Caretaker (adult crew member on same vessel)
    └─ Newborn/Child without caretaker → death after 5 / 15 days
    └─ Teenager without caretaker → efficiency 20 % instead of 50 %

Career funds
    └─ Daily deduction: Newborn 150 / Child 100 / Teenager 50
    └─ Insufficient funds → LOW SUPPORT
          Child efficiency: 25 % → 20 %
          Teenager efficiency: 50 % → 35 %

Genetics (father trait + mother trait)
    └─ Same+Same → 96 % same, 4 % mutation
    └─ Mixed → 48 % / 48 % / 4 % third trait
```

---

## Configuration

All values are in `BreedingConfig.cs`. Recompile to apply changes.

| Field | Default | Description |
|---|---|---|
| `PregnancyDuration` | 30 days | Length of pregnancy |
| `PostpartumDuration` | 10 days | Postpartum recovery |
| `NewbornDuration` | 50 days | Time as Newborn |
| `ChildDuration` | 100 days | Time as Child |
| `TeenagerDuration` | 150 days | Time as Teenager |
| `PregnantEfficiency` | 0.70 | Science lab multiplier while pregnant |
| `PostpartumEfficiency` | 0.50 | Science lab multiplier postpartum |
| `NewbornEfficiency` | 0.00 | Science lab multiplier for Newborn |
| `ChildEfficiency` | 0.25 | Science lab multiplier for Child |
| `TeenagerEfficiency` | 0.50 | Science lab multiplier for Teenager |
| `NoCaretakerTeenagerEfficiency` | 0.20 | Teenager without caretaker |
| `LowSupportChildFactor` | 0.80 | Low Support penalty multiplier for Child |
| `LowSupportTeenagerFactor` | 0.70 | Low Support penalty multiplier for Teenager |
| `NewbornDeathTimer` | 5 days | Time before Newborn dies without caretaker |
| `ChildDeathTimer` | 15 days | Time before Child dies without caretaker |
| `NewbornDailyCost` | 150 | Career funds per Kerbin day (Newborn) |
| `ChildDailyCost` | 100 | Career funds per Kerbin day (Child) |
| `TeenagerDailyCost` | 50 | Career funds per Kerbin day (Teenager) |
| `HabitatMinCrewCapacity` | 4 | Minimum part crew capacity for habitat check |
| `AutoBreeding` | false | Enable fully automatic pairing |
