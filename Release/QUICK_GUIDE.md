# BreedTheKerbal — Quick Guide

## Wymagania
- KSP 1.x (testowane na 1.12)
- Brak zewnętrznych zależności — tylko vanilla KSP

## Instalacja
1. Skopiuj `BreedTheKerbal.dll` i `BreedTheKerbal.cfg` do `GameData/BreedTheKerbal/`
2. Gotowe — przycisk **Colony Manager** pojawia się na pasku narzędzi

---

## Jak zacząć hodować

**Wymagania pojazdu:**
- Part z pojemnością ≥ 4 miejsc
- Science Lab
- Dorosły mężczyzna + dorosła kobieta (nie ciężarna, nie po porodzie)
- ≥ 1 wolne miejsce

**Ręczne parowanie** (domyślne):  
Colony Manager → wybierz parę → **Start Pregnancy**

**Automatyczne:**  
Ustaw `AutoBreeding = true` w `BreedTheKerbal.cfg`

---

## Etapy życia

```
Narodziny → Newborn (50 dni) → Child (100 dni) → Teenager (150 dni) → Adult
```
*(1 dzień Kerbina = 6 h rzeczywistego czasu)*

| Etap | EVA | Lab/Harvester | Pilot |
|---|---|---|---|
| Newborn | ❌ | 0% | 0 gwiazdek |
| Child | ❌ | 25% | 0 gwiazdek |
| Teenager (z opiekunem) | ✅ | 50% | 0 gwiazdek |
| Teenager (bez opiekuna) | ❌ | 20% | 0 gwiazdek |
| Pregnant — 1. połowa | ✅ | 100% | pełna |
| Pregnant — 2. połowa | ❌ | 40% | poziom 1 |
| Postpartum (10 dni) | ❌ | 0% | — |

---

## Opiekun (caretaker)

Newborn i Child **muszą mieć dorosłego** na tym samym pojeździe.

| Etap | Brak opiekuna → |
|---|---|
| Newborn | śmierć po **5 dniach** |
| Child | śmierć po **15 dniach** |
| Teenager | wydajność spada do 20%, EVA zablokowane |

> Czerwony pasek i odliczanie w Colony Manager pokazują czas do śmierci.

---

## Tryb kariery — koszty dzienne

| Etap | Koszt/dzień |
|---|---|
| Newborn | 150 funduszy |
| Child | 100 funduszy |
| Teenager | 50 funduszy |

Brak funduszy → **LOW SUPPORT** → Child 20%, Teenager 35%

---

## Colony Manager

- Hover nad **nazwą Kerbala** → tooltip ze statusem, karą i odliczaniem
- Hover nad **LOW SUPPORT** → szczegóły braku funduszy

---

## Konfiguracja (`BreedTheKerbal.cfg`)

Edytuj i zrestartuj KSP — bez rekompilacji.  
Wszystkie czasy w sekundach Kerbina (`1 dzień = 21 600 s`).

---

## Skróty debugowe (tylko Debug build)

| Skrót | Akcja |
|---|---|
| `Alt + Shift + B` | Wyświetl statusy wszystkich Kerbali |
| `Alt + Shift + N` | Przesuń nie-dorosłych o jeden etap do przodu |
