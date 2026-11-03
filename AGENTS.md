# Loading package work

Use the installed `unity-package` skill for package implementation and validation. Read [ARCHITECTURE.md](ARCHITECTURE.md) and its linked ADR before changing API, dependencies, cancellation or ownership. Read [GLOSSARY.md](GLOSSARY.md) when naming or changing loading concepts.

Preserve existing public contracts, serialized references and GUIDs unless a breaking change is explicitly agreed. Keep Unity and sample UI out of the Contract/App assemblies. Scene cancellation must unlock activation and drain the started operation; consumer rollback is separate.

Package correctness tests belong in `Tests`. Use [Tests/README.md](Tests/README.md) for verification and [MIGRATION.md](MIGRATION.md) for consumer changes. Verify both a development host and a minimal consumer using only declared dependencies. UI Toolkit sample validation includes actual rendering, interaction and disable/re-enable, not compilation alone.

Keep transient logs and work state in the development host. Git publication remains separately authorized.
