---
id: "T0006"
title: "MQTT subscription readiness and per-topic diagnostics"
release: null
priority: 4
size: "M"
state: "todo"
created: "2026-09-25T12:47:43+02:00"
done: null
tags: ["mac-parity"]
---

Mac comparison #18, deferred by user. Track every subscription acknowledgement, expose rejected/missing topics, block commands until all required subscriptions succeed, and reserve Ready for that state. Current UI deliberately reports Connected, not Ready.
