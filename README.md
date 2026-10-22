# Test Cases – Create Booking

## Requirement
A hotel room can be booked for a period (start date – end date) in the future,
if it is not already booked for one or more days during the desired period.

**Abbreviations:** SD = start date, ED = end date, D = today.

**Assumption:** User input always represents valid dates (ensured by date pickers in the UI).

---

## 1. Equivalence Class Testing (ECT)

### Invalid classes
| Class | Description |
|---|---|
| I1 | SD <= today |
| I2 | SD > ED |

### Valid classes (used in the decision table)
It is assumed that there is a single range of fully occupied dates
(all rooms are booked during this period).

| Class | Description | Dates in test data |
|---|---|---|
| B (before) | Dates before the fully occupied range | D+1 … D+3 |
| O (occupied) | Dates in the fully occupied range | D+4 … D+18 |
| A (after) | Dates after the fully occupied range | D+19 … |

The occupied range O is defined by the seed data in `DbInitializer`:
all 3 rooms are booked from D+4 to D+18.

---

## 2. Decision Table-Based Testing (extended entry)

Since SD <= ED, only 6 combinations of classes are possible.

| Test case | 1 | 2 | 3 | 4–5 | 6–7 | 8–10 |
|---|---|---|---|---|---|---|
| SD in | B | A | B | B | O | O |
| ED in | B | A | A | O | A | O |
| Book room | y | y | n | n | n | n |

A booking succeeds only if the whole period lies outside the fully occupied range.

---

## 3. Boundary Value Testing (BVT)

Values of SD and ED are tested at the limits of each class in every column:
- B has one relevant limit: the last day before O (D+3)
- A has one relevant limit: the first day after O (D+19)
- O has two limits: the first day (D+4) and the last day (D+18)

### Test cases from the decision table
| # | SD | ED | Expected result | Reason |
|---|---|---|---|---|
| 1 | D+1 | D+3 | Booking created | ED = last day before O |
| 2 | D+19 | D+19 | Booking created | SD = first day after O |
| 3 | D+3 | D+19 | Booking rejected | Period spans the whole of O |
| 4 | D+3 | D+4 | Booking rejected | ED = first day of O |
| 5 | D+3 | D+18 | Booking rejected | ED = last day of O |
| 6 | D+4 | D+19 | Booking rejected | SD = first day of O |
| 7 | D+18 | D+19 | Booking rejected | SD = last day of O |
| 8 | D+4 | D+4 | Booking rejected | SD and ED = first day of O |
| 9 | D+4 | D+18 | Booking rejected | SD and ED cover all of O |
| 10 | D+18 | D+18 | Booking rejected | SD and ED = last day of O |

### Test cases from the invalid classes
| # | SD | ED | Expected result | Class |
|---|---|---|---|---|
| 11 | D | D+1 | Error (invalid input) | I1 – boundary: SD = today |
| 12 | D+2 | D+1 | Error (invalid input) | I2 – boundary: SD = ED + 1 |

The valid boundary "SD = tomorrow" (D+1) is covered by test case 1.

---

## 4. Traceability

| Test cases | Cucumber (Reqnroll) | Postman | Selenium |
|---|---|---|---|
| 1–12 | All | – | – |
| 1 (success) | ✓ | ✓ | ✓ |
| 4 (failure) | ✓ | ✓ | ✓ |

