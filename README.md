# Flood Escape (Unity 6)

Race against rising hazards across different maps, touch buttons to unlock exit routes, and reach the safe zone before the chamber is submerged.

---

## Goal
Navigate through obstacle courses, find and touch all required buttons in sequence to open blocked paths, and reach the exit zone before the rising hazard catches up to you.

---

## Rules
* **Outrun the Rising Hazard:** Lava, acid, or water continuously rises throughout each stage. Falling into the hazard results in an instant loss.
* **Activate Buttons in Order:** Locked doors, gates, and bridges require you to locate and touch buttons along the route to progress. Buttons follow a color-coded sequence:
  * 🟢 **Green (Active):** Ready to be pressed—touch this button to trigger the next path or unlock the next button.
  * 🟡 **Yellow (Pending):** Locked—not yet its turn to be pressed; activate the current green button first.
  * ⚪ **Grey (Pressed):** Already activated and completed.
* **Precision Platforming:** Narrow wall ledges, floating platforms, and suspended bridges require careful jumps, falling off the main path ends the run.
* **Reach the Exit Zone:** A round is only complete once you cross the final stretch and enter the safe exit area.

---

## Controls
| Action | Input |
| :--- | :--- |
| **Move** | `W` `A` `S` `D` or `Arrow Keys` |
| **Jump** | `Spacebar` |
| **Camera Look** | `Mouse Movement` |

---

## Setup & Installation
1. Clone or download the repository to your local machine.
2. Open **Unity Hub** and add the project folder using **Unity 6000.3.8f1**.
3. Once the project loads, navigate to `Assets/Scenes` in the Project window.
4. Open the **Lobby** scene (starter scene) and press **Play** in the Unity Editor.

---

## Engine & Requirements
* **Unity Version:** `Unity 6000.3.8f1` (Unity 6)
* **Language:** C#
* **Platform:** Windows (Standalone Build)

---

## Maps
* **Volcanic Outpost:** A volcanic stronghold featuring narrow wall ledges, suspended rope bridges, stepping-stone lava crossings, and a final climb to an elevated escape structure.
