# Static finding — bulk rectangle selection and event buffer

Target: `Battle_Realms_F(5).exe` (authoritative BRZE specimen)

## Rectangle candidate list

Global candidate list: `0x879748`.

Rectangle selection appends candidates via `0x4ACB59` at `0x5D44B5`, iterates them, calls AddUnit `0x5A6FD8` at `0x5D455A`, clears the candidate list at `0x5D4573`, then runs sort/rebuild `0x5A719F`.

`0x879748` is initialized with default constructor `0x4ABFBF`, which sets:

- first block `+0x20 = 0x80` (128)
- growth block `+0x24 = 0x80` (128)

Therefore a freeze around ~80 candidates is not explained by candidate-list capacity.

## Selection-add event buffer

`0x552FFA` writes one 4-byte type-0 selection-add event per admitted unit.

Global event-buffer state:

- used bytes: `0x841C94`
- remaining bytes: `0x841C98`
- buffer pointer: `0x841C9C`

Native buffer allocation at `0x550CCB` is `0x100` (256) bytes.

Known 0x100 reset sites:

- initialization: `0x56176A`
- post-flush/reset: `0x561D71`

When fewer than 4 bytes remain, `0x552FFA` calls `0x561B2A` before writing the next event.

Thus a single drag that admits more than 64 units necessarily causes an event-buffer flush in the middle of the rectangle AddUnit loop. This matches the runtime split: multiple small drags work, one large drag freezes.

Next diagnostic: expand the buffer to 0x400 (1024) bytes with a real target-process allocation and patch all three native size/reset sites consistently, while leaving event emission LIVE.
