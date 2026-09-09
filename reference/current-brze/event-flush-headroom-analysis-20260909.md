# Static + runtime finding — event flush headroom

Target: authoritative `Battle_Realms_F(5).exe`.

## Exact 258/-2 explanation

Rectangle selection can call `0x5A7121` before adding the new rectangle selection. `0x5A7121` clears the local selection and emits a 2-byte event when event emission is enabled.

Then each admitted unit goes through `0x5A6FD8 -> 0x552FFA` and emits a 4-byte type-0 selection-add event.

Therefore the decisive telemetry state is arithmetically exact:

- initial clear event: 2 bytes
- 63 selection-add events: 63 * 4 = 252 bytes
- total before the next add: 254 bytes used, 2 bytes remaining
- the 64th selection-add needs 4 bytes, so `0x552FFA` calls `0x561B2A`
- telemetry proves that flush did not reset the queue in this failing case
- producer then continues and writes 4 bytes anyway
- result: used = 258, remaining = -2 (`0xFFFFFFFE`)

This exactly matches the user runtime log.

## Why earlier EventBuffer-1024 is not the right final direction

`0x561B2A` wraps the current event payload in additional metadata before passing a descriptor to `0x66A5A2`. The send helper checks descriptor length against connection state before accepting it. Enlarging the event backing store can merely postpone the native flush and can create a larger downstream packet; it does not address a flush that is rejected when the payload is already too large for the downstream send path.

## Next surgical probe

Keep the physical backing buffer at its native 256 bytes, but reduce the *logical usable event window* so native flush is requested earlier while the accumulated event payload is still materially smaller.

Diagnostic target: 160 logical bytes (`0xA0`) with native 256-byte allocation unchanged.

Patch only the logical remaining-byte initialization/reset sites and arm current remaining to 160 while the queue is empty. Keep UI/SORT/SIM 120 in the separate proven Sim120 specimen. This isolates whether packet headroom is the missing condition for a successful native flush.
