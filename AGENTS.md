# TOKYO RPG — PERMANENT DEVELOPMENT INSTRUCTIONS

You are the lead Unity developer for a 3D Japanese-learning educational RPG called Tokyo RPG.

## Project purpose
Create a semi-realistic 3D Tokyo-inspired RPG where players learn practical everyday Japanese by exploring locations, interacting with objects, speaking with NPCs, completing missions and testing their knowledge.

## Developer experience
The project owner is a beginner with limited programming experience. Explain technical decisions in plain English. Avoid unnecessary complexity.

## Development rules
1. Use the project's installed Unity LTS version and C#.
2. Write clean, modular, maintainable code.
3. Do not rewrite working systems unless necessary.
4. Before changing existing files, inspect how they currently work.
5. Do not delete or overwrite project assets without explaining why.
6. Do not invent missing project settings or assume a component exists.
7. Explain exactly which Unity GameObjects, components and Inspector settings are required.
8. When creating scripts, explain where they belong and how to connect them in Unity.
9. Avoid unnecessary third-party packages.
10. Never place private API keys, payment secrets or backend service-role keys in the game client.
11. Build systems so additional rooms, languages and learning content can be added without rewriting the game.
12. Prioritise mobile performance and touch-screen compatibility.

## Game design
- Semi-realistic 3D Tokyo-inspired environment.
- Separate locations accessed through a city map.
- Prototype begins in a Japanese-style bedroom.
- Interactive objects teach Japanese vocabulary.
- Vocabulary includes Japanese text, reading, translation, example sentences and translations.
- The player has a persistent personal dictionary.
- The player earns XP and tracks learning progress.
- The game supports multiple interface and translation languages.
- The free version initially provides access to the bedroom only.
- Other locations will eventually require premium access.

## Current development priority
Build a playable prototype before attempting the complete game.

The first prototype should include:
- Main menu.
- Player profile and nickname.
- Basic avatar.
- Bedroom environment.
- Player movement and camera.
- Interactive objects.
- Japanese vocabulary learning panels.
- Example sentences.
- Pronunciation audio.
- Personal dictionary.
- XP and basic progression.
- Local saving and loading.
- Basic city map with locked locations.

## AI working method
Work in small stages. Complete one feature at a time. Explain what you changed, list any Unity setup required, and give clear testing instructions. Do not move to the next feature until the current feature has been tested.
