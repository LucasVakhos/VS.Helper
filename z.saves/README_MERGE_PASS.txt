VS.Helper(33) FULL EVOLUTION MERGED

Merged layers:
- ThreadOrchestrator Core
- DTEProxy safe DTE access
- Swarm Memory Store: %AppData%\VS.Helper\swarm.memory.json
- Self Evolving Swarm rules: %AppData%\VS.Helper\swarm.rules.json
- SelfDefense safe-mode guard
- SelfUpgradeCore: VS.Helper-only command, build version increment + build + VSIX install/open
- Commands: RunSwarmCommand, EvolveSwarmCommand, SelfUpgradeCommand

Notes:
- SelfUpgradeCommand is visible only when the opened solution name is VS.Helper.
- All new DTE interactions go through SwitchToMainThreadAsync / DTEProxy.
- VSTHRD109 is avoided in the new swarm core.
