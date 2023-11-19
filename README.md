# R2DeathMaskMod

This repository contains a simple item mod for Risk of Rain 2 which adds a Legendary tier Death Mask.

When an enemy is killed, nearby enemies will die if their health is low enough or be cursed with Death Mark for a short amount of time.

| Item stack | Kill Threshold | Curse duration | AOE Range |
|------------|----------------|----------------|-----------|
| 1 | 25% HP | 7s | 12m |
| 2 | 50% HP | 14s | 16m |
| 3 | 75% HP | 21s | 20m |
| 4 | 100% HP | 28s | 24m |
| ... | ... | ... | ... |
| x | 100% HP | x * 7s | 4 * x + 12m |
