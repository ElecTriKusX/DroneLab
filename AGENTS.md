# DroneLab project instructions

For physics work, first read Docs/Physics/PHYSICS_REFERENCE.md, PARAMETERS.md and VALIDATION.md.
The accepted parameter contract is Tools/generate_physics_contract.py; regenerate DTOs,
schemas and PARAMETERS.md together. Do not silently rename fields or change units.
Keep optional effects and future models in PHYSICS_REFERENCE.md; distinguish contract support from runtime support.
Pure mathematical models live in Assets/DronePhysics/Core without UnityEngine references.
Rigidbody/input/editor adapters live in separate assemblies. Never apply experimental forces
and the new forces simultaneously to the same drone.
Update tests and documentation for physical behavior changes. Do not claim Unity Play Mode
tests passed unless they actually ran in Unity. Update VALIDATION.md with actual results.
Commit Unity .meta files for newly introduced assets and folders.
